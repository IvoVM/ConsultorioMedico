using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure;

// Demo registers and accounts on top of the demo clinic: a register closed four days ago with a card payment
// already settled and reconciled, yesterday's closed with a cash shortfall and a refund, today's open in both
// sites, a payment on account that left credit, and a payment plan with an installment overdue. Everything is
// made up: batches and coupons are invented and the settlement comes from the simulated processor's rules.
// It runs once, when the clinic has no register yet, and does nothing if the demo patients are missing.
public class CashDemoSeeder(TenantDbContext db, TimeZoneInfo zone)
{
    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.CashSessions.AnyAsync(ct))
            return;
        var docs = new[] { "27889900", "22334455", "25990112", "28441990", "45667801", "33221098" };
        var patients = await db.Clients.Where(c => docs.Contains(c.DocumentNumber)).ToDictionaryAsync(c => c.DocumentNumber, ct);
        var locations = await db.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(ct);
        if (patients.Count < docs.Length || locations.Count == 0)
            return;
        var main = locations.FirstOrDefault(l => l.Name != "Anexo Thames") ?? locations[0];
        var annex = locations.FirstOrDefault(l => l.Name == "Anexo Thames");
        var desk = await db.Employees.Where(e => e.DocumentNumber == "27111222").Select(e => (Guid?)e.UserId).FirstOrDefaultAsync(ct);
        var today = SchedulingRules.LocalDate(DateTimeOffset.UtcNow, zone);
        DateTimeOffset At(DateOnly day, int hour, int minute = 0) => SchedulingRules.Combine(day, new TimeOnly(hour, minute), zone);

        // Four days ago: Lucía paid her Galeno copay by Mastercard; it is settled and reconciled below.
        var older = Session(main, At(today.AddDays(-4), 8), 10000m, desk);
        var lucia = (await OpenInvoicesAsync(patients["33221098"].Id, ct)).FirstOrDefault();
        Receipt? luciaCard = null;
        if (lucia is not null)
            luciaCard = Pay(older, patients["33221098"], lucia, Balance(lucia), PaymentMethod.Card, At(today.AddDays(-4), 16, 20), (CardBrand.Mastercard, 1, "0315", "0042"));
        Close(older, At(today.AddDays(-4), 19), counted: 10000m, notes: null);

        // Yesterday: Bruno paid his oldest invoice in cash, there was an expense and part of Camila's credit
        // (from her voided invoice) was given back; the count came up $ 500 short.
        var yesterday = Session(main, At(today.AddDays(-1), 8), 20000m, desk);
        var brunoInvoices = await OpenInvoicesAsync(patients["28441990"].Id, ct);
        if (brunoInvoices.Count > 0)
            Pay(yesterday, patients["28441990"], brunoInvoices[0], Balance(brunoInvoices[0]), PaymentMethod.Cash, At(today.AddDays(-1), 9, 15), null);
        Move(yesterday, CashMovementKind.Expense, PaymentMethod.Cash, 3500m, "Insumos de librería", At(today.AddDays(-1), 12), null, desk);
        if (await CreditAsync(patients["45667801"].Id, ct) >= 10000m)
            Refund(yesterday, patients["45667801"], 10000m, "Devolución parcial del comprobante anulado", At(today.AddDays(-1), 17, 30), desk);
        var expected = CashRules.ExpectedCash(yesterday.OpeningCash, db.CashMovements.Local.Where(m => m.CashSessionId == yesterday.Id).Select(m => (m.Kind, m.Method, m.Amount)));
        Close(yesterday, At(today.AddDays(-1), 19), counted: expected - 500m, notes: "Faltan $ 500. Se revisa con la recepción de la mañana.");

        // Today: Nora pays by Visa in three installments, Diego leaves money on account and a certificate is sold.
        var open = Session(main, At(today, 8), 15000m, desk);
        var nora = (await OpenInvoicesAsync(patients["22334455"].Id, ct)).FirstOrDefault();
        if (nora is not null)
            Pay(open, patients["22334455"], nora, Balance(nora), PaymentMethod.Card, At(today, 9, 40), (CardBrand.Visa, 3, "0042", "1187"));
        OnAccount(open, patients["25990112"], 10000m, PaymentMethod.Transfer, "Pago a cuenta de la próxima consulta", At(today, 10, 5), desk);
        Move(open, CashMovementKind.Income, PaymentMethod.Cash, 5000m, "Certificado de aptitud física", At(today, 11), null, desk);
        if (annex is not null)
            Session(annex, At(today, 9), 8000m, desk);

        // Bruno's other balance split in four monthly installments; the first one fell due and is unpaid.
        var bruno = brunoInvoices.Skip(1).FirstOrDefault();
        if (bruno is not null && Balance(bruno) > 0)
        {
            var balance = Balance(bruno);
            var plan = new PaymentPlan { Id = Guid.NewGuid(), InvoiceId = bruno.Id, CreatedAt = At(today.AddDays(-25), 10), Amount = balance, PaidBefore = bruno.Payments.Sum(p => p.Amount) };
            foreach (var (number, due, amount) in PlanRules.Split(balance, 4, today.AddDays(-20)))
                plan.Installments.Add(new PlanInstallment { Id = Guid.NewGuid(), PlanId = plan.Id, Number = number, DueDate = due, Amount = amount });
            db.PaymentPlans.Add(plan);
        }

        // The Mastercard settlement of last week, as the simulated processor would send it: fee kept, paid two days later.
        if (luciaCard is not null)
        {
            var charged = today.AddDays(-4);
            var fee = decimal.Round(luciaCard.Amount * 0.018m, 2, MidpointRounding.AwayFromZero);
            var settlement = new CardSettlement { Id = Guid.NewGuid(), Brand = CardBrand.Mastercard, From = today.AddDays(-7), To = today.AddDays(-1), ImportedAt = At(today, 8, 30), Simulated = true };
            var line = new CardSettlementLine
            {
                Id = Guid.NewGuid(),
                SettlementId = settlement.Id,
                Batch = "0315",
                Coupon = "0042",
                Installments = 1,
                ChargedOn = charged,
                PaidOn = charged.AddDays(2),
                Gross = luciaCard.Amount,
                Fee = fee,
                Net = luciaCard.Amount - fee,
                ReceiptId = luciaCard.Id
            };
            settlement.Lines.Add(line);
            luciaCard.SettlementLineId = line.Id;
            db.CardSettlements.Add(settlement);
        }

        await db.SaveChangesAsync(ct);
    }

    private CashSession Session(Location location, DateTimeOffset openedAt, decimal opening, Guid? userId)
    {
        var session = new CashSession { Id = Guid.NewGuid(), LocationId = location.Id, OpeningCash = opening, OpenedAt = openedAt, OpenedById = userId };
        db.CashSessions.Add(session);
        return session;
    }

    private void Close(CashSession session, DateTimeOffset closedAt, decimal counted, string? notes)
    {
        var movements = db.CashMovements.Local.Where(m => m.CashSessionId == session.Id).Select(m => (m.Kind, m.Method, m.Amount));
        session.ExpectedCash = CashRules.ExpectedCash(session.OpeningCash, movements);
        session.CountedCash = counted;
        session.Notes = notes;
        session.Status = CashSessionStatus.Closed;
        session.ClosedAt = closedAt;
        session.ClosedById = session.OpenedById;
    }

    private void Move(CashSession session, CashMovementKind kind, PaymentMethod method, decimal amount, string description, DateTimeOffset at, Guid? receiptId, Guid? userId) =>
        db.CashMovements.Add(new CashMovement { Id = Guid.NewGuid(), CashSessionId = session.Id, Kind = kind, Method = method, Amount = amount, Description = description, At = at, ReceiptId = receiptId, UserId = userId });

    private Receipt Pay(CashSession session, Client patient, Invoice invoice, decimal amount, PaymentMethod method, DateTimeOffset at, (CardBrand Brand, int Installments, string Batch, string Coupon)? card)
    {
        var receipt = NewReceipt(session, patient, amount, method, at, null, card);
        var payment = new InvoicePayment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, ReceiptId = receipt.Id, Amount = amount, Method = method, PaidAt = at };
        db.InvoicePayments.Add(payment);
        invoice.Payments.Add(payment);
        var paid = invoice.Payments.Sum(p => p.Amount);
        invoice.Status = BillingRules.StatusFor(invoice.Total, invoice.InsurerAmount, paid);
        invoice.PaidAt = invoice.Status == InvoiceStatus.Paid ? at : null;
        Move(session, CashMovementKind.Payment, method, amount, $"Cobro a {patient.FirstName} {patient.LastName}", at, receipt.Id, session.OpenedById);
        return receipt;
    }

    private void OnAccount(CashSession session, Client patient, decimal amount, PaymentMethod method, string note, DateTimeOffset at, Guid? userId)
    {
        var receipt = NewReceipt(session, patient, amount, method, at, note, null);
        Move(session, CashMovementKind.Payment, method, amount, $"Cobro a {patient.FirstName} {patient.LastName}", at, receipt.Id, userId);
    }

    private void Refund(CashSession session, Client patient, decimal amount, string note, DateTimeOffset at, Guid? userId)
    {
        var receipt = NewReceipt(session, patient, -amount, PaymentMethod.Cash, at, note, null);
        Move(session, CashMovementKind.Refund, PaymentMethod.Cash, amount, $"Devolución a {patient.FirstName} {patient.LastName}", at, receipt.Id, userId);
    }

    private Receipt NewReceipt(CashSession session, Client patient, decimal amount, PaymentMethod method, DateTimeOffset at, string? note, (CardBrand Brand, int Installments, string Batch, string Coupon)? card)
    {
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ClientId = patient.Id,
            Amount = amount,
            Method = method,
            PaidAt = at,
            LocationId = session.LocationId,
            CashSessionId = session.Id,
            Note = note,
            CardBrand = card?.Brand,
            CardInstallments = card?.Installments,
            CardBatch = card?.Batch,
            CardCoupon = card?.Coupon
        };
        db.Receipts.Add(receipt);
        return receipt;
    }

    private async Task<List<Invoice>> OpenInvoicesAsync(Guid clientId, CancellationToken ct) =>
        await db.Invoices.Include(i => i.Payments)
            .Where(i => i.ClientId == clientId && (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid) && i.Total > 0)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    // Money received that no live invoice used, as the account works it out.
    private async Task<decimal> CreditAsync(Guid clientId, CancellationToken ct)
    {
        var received = await db.Receipts.Where(r => r.ClientId == clientId).SumAsync(r => r.Amount, ct);
        var applied = await db.InvoicePayments.Where(p => p.Invoice!.ClientId == clientId && p.Invoice.Status != InvoiceStatus.Voided && p.ReceiptId != null).SumAsync(p => p.Amount, ct);
        return received - applied;
    }

    private static decimal Balance(Invoice invoice) => BillingRules.Balance(invoice.Total, invoice.InsurerAmount, invoice.Payments.Sum(p => p.Amount));
}
