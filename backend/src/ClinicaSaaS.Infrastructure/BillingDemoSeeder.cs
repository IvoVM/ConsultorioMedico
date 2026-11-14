using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure;

// Demo billing on top of the demo clinic: invoices with several items, a discount, partial payments, an insurer's share
// with the patient's copay, a voided one with its credit note, and fiscal numbers from the simulator.
// Everything is made up: the CUIT 30-00000000-7 belongs to no one, every CAE is flagged as simulated, and nothing
// is ever sent to ARCA. It runs once, also on a database seeded before billing had these features, and does nothing
// when the clinic already has invoices like these or the demo patients are missing.
public class BillingDemoSeeder(TenantDbContext db, TimeZoneInfo zone, string clinicName)
{
    public const string DemoTaxId = "30000000007";
    private const int DemoPointOfSale = 1;
    private int invoiceNumber;
    private int creditNoteNumber;
    private int coupon;

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.Invoices.AnyAsync(i => i.Number != null || i.Discount > 0 || i.InsurerAmount > 0, ct))
            return;

        var patients = await db.Clients
            .Where(c => new[] { "30111222", "28441990", "45667801", "33221098", "22334455", "36778821" }.Contains(c.DocumentNumber))
            .ToDictionaryAsync(c => c.DocumentNumber, ct);
        var doctors = await db.Users
            .Where(u => new[] { "medico@demo.local", "elena.vazquez@demo.local", "julian.costa@demo.local", "sofia.herrera@demo.local" }.Contains(u.Email!))
            .ToDictionaryAsync(u => u.Email!, ct);
        var types = await db.AppointmentTypes
            .Where(t => new[] { "Consulta clínica", "Consulta pediátrica", "Consulta traumatológica", "Electrocardiograma" }.Contains(t.Name))
            .ToDictionaryAsync(t => t.Name, ct);
        var insurers = await db.Insurers
            .Where(i => new[] { "OSDE", "Medicus", "Galeno", "PAMI" }.Contains(i.Name))
            .ToDictionaryAsync(i => i.Name, ct);
        if (patients.Count < 6 || doctors.Count < 4 || types.Count < 4 || insurers.Count < 4)
            return;

        // A new database has no settings row yet; the app would create it on first read with these same defaults.
        var settings = await db.ClinicSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new ClinicSettings { Id = Guid.NewGuid(), Name = clinicName };
            db.ClinicSettings.Add(settings);
        }
        if (settings.TaxId is null)
        {
            settings.FiscalEnabled = true;
            settings.TaxId = DemoTaxId;
            settings.TaxCondition = TaxCondition.Monotributista;
            settings.PointOfSale = DemoPointOfSale;
        }

        var today = SchedulingRules.LocalDate(DateTimeOffset.UtcNow, zone);
        AddFee(types["Consulta clínica"], "OSDE", 18000m, 6000m, today);
        AddFee(types["Consulta traumatológica"], "Medicus", 22000m, 6000m, today);
        AddFee(types["Electrocardiograma"], "Galeno", 12000m, 6000m, today);
        AddFee(types["Electrocardiograma"], "PAMI", 18000m, null, today);

        var martin = doctors["medico@demo.local"];
        var elena = doctors["elena.vazquez@demo.local"];
        var julian = doctors["julian.costa@demo.local"];
        var sofia = doctors["sofia.herrera@demo.local"];

        // Covered by OSDE, the patient paid the copay by card, authorized.
        var osde = await VisitAsync(patients["30111222"], martin, types["Consulta clínica"], today.AddDays(-3), insurers["OSDE"], "Control anual", ct);
        if (osde is not null)
        {
            var invoice = Invoice(osde, [("Consulta clínica", 1, 24000m)], discount: 0, insurer: "OSDE", insurerAmount: 18000m);
            Pay(invoice, osde, 6000m, PaymentMethod.Card, osde.End.AddMinutes(5));
            Authorize(invoice, FiscalVoucherType.FacturaC, osde.End.AddMinutes(10));
        }

        // Two items and a discount; the patient paid part in cash and owes the rest.
        var partial = await VisitAsync(patients["28441990"], martin, types["Consulta clínica"], today.AddDays(-5), null, "Certificado para el trabajo", ct);
        if (partial is not null)
        {
            var invoice = Invoice(partial, [("Consulta clínica", 1, 25000m), ("Certificado médico", 1, 3000m)], discount: 2000m, insurer: null, insurerAmount: 0);
            Pay(invoice, partial, 10000m, PaymentMethod.Cash, partial.End.AddMinutes(5));
        }

        // A procedure on top of the visit, split between Medicus and the patient, paid in two parts and authorized.
        var knee = await VisitAsync(patients["36778821"], julian, types["Consulta traumatológica"], today.AddDays(-12), insurers["Medicus"], "Dolor de rodilla", ct);
        if (knee is not null)
        {
            var invoice = Invoice(knee, [("Consulta traumatológica", 1, 28000m), ("Infiltración de rodilla", 2, 9000m)], discount: 0, insurer: "Medicus", insurerAmount: 30000m);
            Pay(invoice, knee, 10000m, PaymentMethod.Transfer, knee.End.AddMinutes(5));
            Pay(invoice, knee, 6000m, PaymentMethod.Cash, knee.End.AddDays(3));
            Authorize(invoice, FiscalVoucherType.FacturaC, knee.End.AddMinutes(10));
        }

        // Authorized and then voided, so it carries a credit note; the payment stays on record to be returned.
        var voided = await VisitAsync(patients["45667801"], elena, types["Consulta pediátrica"], today.AddDays(-2), null, "Fiebre", ct);
        if (voided is not null)
        {
            var invoice = Invoice(voided, [("Consulta pediátrica", 1, 24000m)], discount: 0, insurer: null, insurerAmount: 0);
            Pay(invoice, voided, 24000m, PaymentMethod.Cash, voided.End.AddMinutes(5));
            Authorize(invoice, FiscalVoucherType.FacturaC, voided.End.AddMinutes(10));
            invoice.Status = InvoiceStatus.Voided;
            invoice.VoidedAt = voided.End.AddHours(2);
            invoice.VoidReason = "Se emitió a nombre de otro paciente.";
            invoice.CreditNoteType = FiscalVoucherType.NotaCreditoC;
            invoice.CreditNoteNumber = ++creditNoteNumber;
            invoice.CreditNoteCae = FakeCae(500 + creditNoteNumber);
        }

        // Galeno covers part; the patient has not paid the copay yet.
        var pending = await VisitAsync(patients["33221098"], sofia, types["Electrocardiograma"], today.AddDays(-7), insurers["Galeno"], "Control de palpitaciones", ct);
        if (pending is not null)
            Invoice(pending, [("Electrocardiograma", 1, 18000m)], discount: 0, insurer: "Galeno", insurerAmount: 12000m);

        // PAMI covers it all, so the patient owes nothing.
        var covered = await VisitAsync(patients["22334455"], sofia, types["Electrocardiograma"], today.AddDays(-13), insurers["PAMI"], "Control de presión", ct);
        if (covered is not null)
            Invoice(covered, [("Electrocardiograma", 1, 18000m)], discount: 0, insurer: "PAMI", insurerAmount: 18000m);

        await db.SaveChangesAsync(ct);
    }

    private void AddFee(AppointmentType type, string payer, decimal amount, decimal? copay, DateOnly today)
    {
        if (db.Fees.Any(f => f.AppointmentTypeId == type.Id && f.Payer == payer))
            return;
        db.Fees.Add(new Fee
        {
            Id = Guid.NewGuid(),
            AppointmentTypeId = type.Id,
            Payer = payer,
            Amount = amount,
            Copay = copay,
            EffectiveFrom = new DateOnly(today.Year, 1, 1)
        });
    }

    // A completed visit on a slot the professional has free that day, or none if the day is full.
    private async Task<Appointment?> VisitAsync(Client patient, Identity.TenantUser professional, AppointmentType type, DateOnly day, Insurer? insurer, string reason, CancellationToken ct)
    {
        var location = await db.ScheduleBlocks
            .Where(b => b.ProfessionalId == professional.Id && b.AppointmentTypeId == type.Id)
            .Select(b => (Guid?)b.LocationId)
            .FirstOrDefaultAsync(ct)
            ?? await db.Locations.OrderBy(l => l.Name).Select(l => l.Id).FirstAsync(ct);
        var dayStart = SchedulingRules.Combine(day, new TimeOnly(0, 0), zone);
        var busy = await db.Appointments
            .Where(a => a.ProfessionalId == professional.Id && a.Start < dayStart.AddDays(1) && a.End > dayStart)
            .Select(a => new { a.Start, a.End })
            .ToListAsync(ct);
        busy.AddRange(db.Appointments.Local
            .Where(a => a.ProfessionalId == professional.Id && a.Start < dayStart.AddDays(1) && a.End > dayStart)
            .Select(a => new { a.Start, a.End }));
        for (var minutes = 8 * 60; minutes + type.DurationMinutes <= 21 * 60; minutes += 10)
        {
            var start = SchedulingRules.Combine(day, new TimeOnly(minutes / 60, minutes % 60), zone);
            var end = start.AddMinutes(type.DurationMinutes);
            if (busy.Any(b => b.Start < end && b.End > start))
                continue;
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                ClientId = patient.Id,
                ProfessionalId = professional.Id,
                LocationId = location,
                AppointmentTypeId = type.Id,
                Start = start,
                End = end,
                Status = AppointmentStatus.Completed,
                VisitReason = reason,
                InsurerId = insurer?.Id,
                MemberNumber = insurer is null ? null : "DEMO-0001",
                CoverageAccepted = insurer is null ? null : true
            };
            db.Appointments.Add(appointment);
            db.Encounters.Add(new Encounter
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                ClientId = patient.Id,
                ProfessionalId = professional.Id,
                SpecialtyId = type.SpecialtyId,
                Note = "Consulta de demostración.",
                IsClosed = true,
                CreatedAt = start
            });
            return appointment;
        }
        return null;
    }

    private Invoice Invoice(Appointment appointment, (string Description, int Quantity, decimal UnitPrice)[] lines, decimal discount, string? insurer, decimal insurerAmount)
    {
        var total = BillingRules.Total(lines.Select(l => l.Quantity * l.UnitPrice)) - discount;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            ClientId = appointment.ClientId,
            Total = total,
            Discount = discount,
            Insurer = insurer,
            InsurerAmount = insurerAmount,
            Status = BillingRules.StatusFor(total, insurerAmount, 0),
            CreatedAt = appointment.End
        };
        if (invoice.Status == InvoiceStatus.Paid)
            invoice.PaidAt = appointment.End;
        db.Invoices.Add(invoice);
        for (var i = 0; i < lines.Length; i++)
        {
            db.InvoiceItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Position = i,
                Description = lines[i].Description,
                Quantity = lines[i].Quantity,
                UnitPrice = lines[i].UnitPrice,
                Amount = lines[i].Quantity * lines[i].UnitPrice
            });
        }
        return invoice;
    }

    // Each payment is a receipt applied in full to the invoice. Card ones carry a made-up batch and coupon.
    private void Pay(Invoice invoice, Appointment visit, decimal amount, PaymentMethod method, DateTimeOffset at)
    {
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ClientId = invoice.ClientId,
            Amount = amount,
            Method = method,
            PaidAt = at,
            LocationId = visit.LocationId,
            CardBrand = method == PaymentMethod.Card ? CardBrand.Visa : null,
            CardInstallments = method == PaymentMethod.Card ? 1 : null,
            CardBatch = method == PaymentMethod.Card ? "DEMO01" : null,
            CardCoupon = method == PaymentMethod.Card ? $"{++coupon:0000}" : null
        };
        db.Receipts.Add(receipt);
        db.InvoicePayments.Add(new InvoicePayment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, ReceiptId = receipt.Id, Amount = amount, Method = method, PaidAt = at });
        var paid = BillingRules.Total(db.InvoicePayments.Local.Where(p => p.InvoiceId == invoice.Id).Select(p => p.Amount));
        invoice.Status = BillingRules.StatusFor(invoice.Total, invoice.InsurerAmount, paid);
        invoice.PaidAt = invoice.Status == InvoiceStatus.Paid ? at : null;
    }

    private void Authorize(Invoice invoice, FiscalVoucherType type, DateTimeOffset at)
    {
        invoice.VoucherType = type;
        invoice.PointOfSale = DemoPointOfSale;
        invoice.Number = ++invoiceNumber;
        invoice.Cae = FakeCae(invoiceNumber);
        invoice.CaeDueDate = SchedulingRules.LocalDate(at, zone).AddDays(10);
        invoice.AuthorizedAt = at;
        invoice.FiscalSimulated = true;
    }

    // Starts with nines so nobody mistakes it for a real CAE.
    private static string FakeCae(int n) => $"9999{n:0000000000}";
}
