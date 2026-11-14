using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class InvoiceService(
    IBillingStore store,
    IAppointmentStore appointments,
    IOrganizationStore organization,
    IClientStore clients,
    IClinicSettingsStore settings,
    IFiscalAuthority fiscal,
    ICurrentUser currentUser,
    IAuditStore audit,
    ITimeZoneProvider timeZone,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<InvoiceDto>> InvoicesAsync(CancellationToken ct)
    {
        var invoices = await store.Invoices
            .OrderByDescending(i => i.CreatedAt)
            .Select(InvoiceMappingExtensions.MapForInvoiceDto())
            .ToListAsync(ct);
        return invoices.Select(invoice => invoice.MapToInvoiceDto()).ToList();
    }

    // Search reads the patient, their document, the total or the voucher number; the status is a filter
    // sent as an InvoiceStatus value, so no label is compared here.
    public async Task<object> InvoicesResultAsync(ListQuery query, bool voided, CancellationToken ct)
    {
        if (!query.Paged) return await InvoicesAsync(ct);
        var source = voided
            ? store.Invoices.Where(i => i.Status == InvoiceStatus.Voided)
            : store.Invoices.Where(i => i.Status != InvoiceStatus.Voided);
        if (Enum.TryParse<InvoiceStatus>(query.Filter, out var status))
            source = source.Where(i => i.Status == status);
        if (query.Term is string term)
            source = source.Where(i =>
                (i.Client.FirstName + " " + i.Client.LastName).ToLower().Contains(term) ||
                i.Client.DocumentNumber.Contains(term) ||
                i.Total.ToString().Contains(term) ||
                (i.Number != null && i.Number.ToString()!.Contains(term)));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("total", false) => source.OrderBy(i => i.Total).ThenBy(i => i.Client.LastName),
            ("total", true) => source.OrderByDescending(i => i.Total).ThenBy(i => i.Client.LastName),
            ("status", false) => source.OrderBy(i => i.Status).ThenBy(i => i.Client.LastName),
            ("status", true) => source.OrderByDescending(i => i.Status).ThenBy(i => i.Client.LastName),
            ("number", false) => source.OrderBy(i => i.Number == null).ThenBy(i => i.Number).ThenBy(i => i.CreatedAt),
            ("number", true) => source.OrderBy(i => i.Number == null).ThenByDescending(i => i.Number).ThenByDescending(i => i.CreatedAt),
            ("date", false) => source.OrderBy(i => i.CreatedAt),
            ("date", true) => source.OrderByDescending(i => i.CreatedAt),
            ("voided", false) => source.OrderBy(i => i.VoidedAt),
            ("voided", true) => source.OrderByDescending(i => i.VoidedAt),
            ("patient", true) => source.OrderByDescending(i => i.Client.LastName).ThenByDescending(i => i.Client.FirstName),
            _ => source.OrderBy(i => i.Client.LastName).ThenBy(i => i.Client.FirstName)
        };
        var page = await Pages.Materialize(ordered.Select(InvoiceMappingExtensions.MapForInvoiceDto()), query, ct);
        return new PageDto<InvoiceDto>(page.Items.Select(invoice => invoice.MapToInvoiceDto()).ToList(), page.Total, page.Page, page.PageSize);
    }

    public async Task<IReadOnlyList<InvoiceDto>> MineAsync(CancellationToken ct)
    {
        var clientId = await OwnClientAsync(ct);
        var invoices = await store.Invoices
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(InvoiceMappingExtensions.MapForInvoiceDto())
            .ToListAsync(ct);
        return invoices.Select(invoice => invoice.MapToInvoiceDto()).ToList();
    }

    public async Task<FileDto> PdfAsync(Guid id, CancellationToken ct)
    {
        var invoice = await ReadAsync(id, ct);
        if (currentUser.Role == AppRoles.Patient && invoice.PatientId != await OwnClientAsync(ct))
            throw new NotFoundException("Comprobante no encontrado.");
        var clinic = await settings.GetAsync(ct);
        var name = invoice.Number is int number
            ? $"comprobante-{invoice.PointOfSale:0000}-{number:00000000}.pdf"
            : $"comprobante-{invoice.Id.ToString()[..8]}.pdf";
        return new FileDto(InvoicePdf.Render(invoice, clinic, timeZone.Zone), "application/pdf", name);
    }

    public async Task<InvoiceDto> UpdateAsync(Guid id, UpdateInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (!BillingRules.CanEdit(invoice))
            throw new BusinessRuleException("Solo se corrige un comprobante pendiente, sin pagos y sin autorizar.");
        var lines = (command.Items ?? []).Select(i => ((i.Description ?? "").Trim(), i.Quantity, BillingRules.NormalizePrice(i.UnitPrice))).ToList();
        var discount = BillingRules.NormalizePrice(command.Discount);
        var insurerAmount = BillingRules.NormalizePrice(command.InsurerAmount);
        if (BillingRules.EditError(lines, discount, insurerAmount) is string error)
            throw new BusinessRuleException(error);
        if (lines.Any(l => l.Item1.Length > 160))
            throw new BusinessRuleException("La descripción de un ítem no puede pasar los 160 caracteres.");

        store.RemoveItems(invoice.Items.ToList());
        invoice.Items.Clear();
        var position = 0;
        foreach (var (description, quantity, unitPrice) in lines)
        {
            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Position = position++,
                Description = description,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Amount = BillingRules.NormalizePrice(quantity * unitPrice)
            };
            store.AddItem(item);
        }
        var subtotal = BillingRules.Total(lines.Select(l => BillingRules.NormalizePrice(l.Item2 * l.Item3)));
        invoice.Discount = discount;
        invoice.Total = subtotal - discount;
        invoice.InsurerAmount = invoice.Insurer is null ? 0 : insurerAmount;
        invoice.Status = BillingRules.StatusFor(invoice.Total, invoice.InsurerAmount, 0);
        invoice.PaidAt = invoice.Status == InvoiceStatus.Paid ? clock.GetUtcNow() : null;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "edicion", "Comprobante", invoice.Id.ToString(), $"total {invoice.Total:0.00} · descuento {discount:0.00}", ct);
        return await ReadAsync(invoice.Id, ct);
    }

    public async Task<InvoiceDto> PayAsync(Guid id, PayInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (!BillingRules.CanPay(invoice.Status))
            throw new BusinessRuleException("Ese comprobante ya no se puede cobrar.");
        var paid = BillingRules.Total(invoice.Payments.Select(p => p.Amount));
        var balance = BillingRules.Balance(invoice.Total, invoice.InsurerAmount, paid);
        var amount = BillingRules.NormalizePrice(command.Amount ?? balance);
        if (BillingRules.PaymentError(amount, balance) is string error)
            throw new BusinessRuleException(error);

        var now = clock.GetUtcNow();
        var payment = new InvoicePayment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, Amount = amount, Method = command.Method, PaidAt = now };
        store.AddPayment(payment);
        invoice.Status = BillingRules.StatusFor(invoice.Total, invoice.InsurerAmount, paid + amount);
        if (invoice.Status == InvoiceStatus.Paid)
            invoice.PaidAt = now;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "pago", "Comprobante", invoice.Id.ToString(), $"{command.Method} {amount:0.00}", ct);
        return await ReadAsync(invoice.Id, ct);
    }

    public async Task<InvoiceDto> AuthorizeAsync(Guid id, CancellationToken ct)
    {
        var clinic = await FiscalSettingsAsync(ct);
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (invoice.Number is not null)
            throw new ConflictException("Ese comprobante ya está autorizado.");
        if (!BillingRules.CanAuthorize(invoice))
            throw new BusinessRuleException("Solo se autoriza un comprobante vigente con importe mayor a 0.");
        var document = await clients.Clients.Where(c => c.Id == invoice.ClientId).Select(c => c.DocumentNumber).FirstAsync(ct);
        var type = BillingRules.VoucherFor(clinic.TaxCondition!.Value);
        var today = Today();
        var authorization = await fiscal.AuthorizeAsync(new FiscalRequest(clinic.TaxId!, clinic.PointOfSale!.Value, type, invoice.Total, document, today), ct);

        invoice.VoucherType = type;
        invoice.PointOfSale = clinic.PointOfSale;
        invoice.Number = authorization.Number;
        invoice.Cae = authorization.Cae;
        invoice.CaeDueDate = authorization.CaeDueDate;
        invoice.AuthorizedAt = clock.GetUtcNow();
        invoice.FiscalSimulated = authorization.Simulated;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "autorizacion", "Comprobante", invoice.Id.ToString(), $"{type} {invoice.PointOfSale:0000}-{invoice.Number:00000000} CAE {invoice.Cae}", ct);
        return await ReadAsync(invoice.Id, ct);
    }

    // Voiding keeps the invoice and its payments on record; an authorized one is cancelled with a credit note.
    public async Task<InvoiceDto> VoidAsync(Guid id, VoidInvoiceCommand command, CancellationToken ct)
    {
        var reason = (command.Reason ?? "").Trim();
        if (reason.Length == 0)
            throw new BusinessRuleException("Indicá por qué se anula.");
        if (reason.Length > 300)
            throw new BusinessRuleException("El motivo no puede pasar los 300 caracteres.");
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (invoice.Status == InvoiceStatus.Voided)
            throw new ConflictException("Ese comprobante ya está anulado.");

        if (invoice.Number is not null && invoice.VoucherType is FiscalVoucherType type)
        {
            var clinic = await FiscalSettingsAsync(ct);
            var document = await clients.Clients.Where(c => c.Id == invoice.ClientId).Select(c => c.DocumentNumber).FirstAsync(ct);
            var noteType = BillingRules.CreditNoteFor(type);
            var note = await fiscal.AuthorizeAsync(new FiscalRequest(clinic.TaxId!, invoice.PointOfSale!.Value, noteType, invoice.Total, document, Today()), ct);
            invoice.CreditNoteType = noteType;
            invoice.CreditNoteNumber = note.Number;
            invoice.CreditNoteCae = note.Cae;
        }
        invoice.Status = InvoiceStatus.Voided;
        invoice.VoidedAt = clock.GetUtcNow();
        invoice.VoidReason = reason;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "anulacion", "Comprobante", invoice.Id.ToString(), reason, ct);
        return await ReadAsync(invoice.Id, ct);
    }

    // Closing a consultation only bills it when the clinic issues invoices and wants them made on its own.
    public async Task IssueOnCloseAsync(Appointment appointment, CancellationToken ct)
    {
        var clinic = await settings.GetAsync(ct);
        if (clinic.BillingEnabled && clinic.AutoInvoice)
            await CreateIfMissingAsync(appointment, ct);
    }

    public async Task<InvoiceDto> IssueAsync(IssueInvoiceCommand command, CancellationToken ct)
    {
        if (!(await settings.GetAsync(ct)).BillingEnabled)
            throw new BusinessRuleException("La clínica no tiene habilitada la facturación.");
        var appointment = await appointments.GetAsync(command.AppointmentId, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.Status != AppointmentStatus.Completed)
            throw new BusinessRuleException("Solo se factura un turno atendido.");
        var invoice = await CreateIfMissingAsync(appointment, ct) ?? throw new ConflictException("Ese turno ya tiene comprobante.");
        await audit.RecordAsync(currentUser.Id, "alta", "Comprobante", invoice.Id.ToString(), null, ct);
        return await ReadAsync(invoice.Id, ct);
    }

    private async Task<Invoice?> CreateIfMissingAsync(Appointment appointment, CancellationToken ct)
    {
        if (await store.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id, ct))
            return null;
        var today = Today();
        string? insurance = null;
        if (appointment.InsurerId is Guid insurerId && appointment.CoverageAccepted != false)
            insurance = await organization.Insurers.Where(i => i.Id == insurerId).Select(i => i.Name).FirstOrDefaultAsync(ct);
        var fees = await store.Fees
            .Where(f => f.AppointmentTypeId == appointment.AppointmentTypeId && f.EffectiveFrom <= today)
            .Select(f => new { f.Payer, f.Amount, f.Copay, f.EffectiveFrom, f.Id, f.AppointmentType.Name })
            .ToListAsync(ct);
        var covered = insurance is null ? [] : fees.Where(f => SamePayer(f.Payer, insurance)).ToList();
        var byInsurer = covered.Count > 0;
        if (!byInsurer)
            covered = fees.Where(f => BillingRules.CanonicalPayer(f.Payer) is null).ToList();
        var chosen = covered.OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).FirstOrDefault();
        // With an insurer's price, the insurer pays the fee and the patient pays the copay on top.
        var insurerAmount = byInsurer && chosen is not null ? chosen.Amount : 0;
        var amount = BillingRules.NormalizePrice((chosen?.Amount ?? 0) + (byInsurer ? chosen?.Copay ?? 0 : 0));
        return await store.AddInvoiceAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            ClientId = appointment.ClientId,
            Total = amount,
            Insurer = byInsurer ? insurance : null,
            InsurerAmount = insurerAmount,
            Status = BillingRules.StatusFor(amount, insurerAmount, 0),
            PaidAt = BillingRules.StatusFor(amount, insurerAmount, 0) == InvoiceStatus.Paid ? clock.GetUtcNow() : null,
            CreatedAt = clock.GetUtcNow(),
            Items =
            [
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Position = 0,
                    Description = chosen?.Name ?? "Consulta",
                    Quantity = 1,
                    UnitPrice = amount,
                    Amount = amount
                }
            ]
        }, ct);
    }

    private async Task<InvoiceDto> ReadAsync(Guid id, CancellationToken ct)
    {
        var invoice = await store.Invoices.Where(i => i.Id == id).Select(InvoiceMappingExtensions.MapForInvoiceDto()).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Comprobante no encontrado.");
        return invoice.MapToInvoiceDto();
    }

    private async Task<ClinicSettings> FiscalSettingsAsync(CancellationToken ct)
    {
        var clinic = await settings.GetAsync(ct);
        if (!clinic.FiscalEnabled)
            throw new BusinessRuleException("La clínica no tiene habilitada la factura electrónica.");
        if (!BillingRules.IsValidTaxId(clinic.TaxId) || clinic.TaxCondition is null || clinic.PointOfSale is null)
            throw new BusinessRuleException("Faltan el CUIT, la condición fiscal o el punto de venta en la configuración.");
        return clinic;
    }

    private async Task<Guid> OwnClientAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? Guid.Empty;
        var own = await clients.Clients.Where(c => c.UserId == userId).Select(c => c.Id).FirstOrDefaultAsync(ct);
        return own == Guid.Empty ? throw new NotFoundException("No hay un paciente asociado a esta cuenta.") : own;
    }

    private DateOnly Today() => SchedulingRules.LocalDate(clock.GetUtcNow(), timeZone.Zone);

    private static bool SamePayer(string? feePayer, string? insurance) =>
        string.Equals(BillingRules.CanonicalPayer(feePayer), BillingRules.CanonicalPayer(insurance), StringComparison.OrdinalIgnoreCase);
}
