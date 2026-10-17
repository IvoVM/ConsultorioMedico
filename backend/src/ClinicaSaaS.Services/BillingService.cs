using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class BillingService(
    IBillingStore store,
    IOrganizationStore organization,
    ICurrentUser currentUser,
    IAuditStore audit,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<FeeDto>> FeesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var fees = await store.Fees
            .Where(f => f.EffectiveFrom <= today)
            .OrderByDescending(f => f.EffectiveFrom)
            .ThenByDescending(f => f.Id)
            .ToFeeDtos()
            .ToListAsync(ct);
        return fees
            .GroupBy(f => f.AppointmentTypeId)
            .Select(g => g.First())
            .OrderBy(f => f.AppointmentType)
            .ToList();
    }

    public async Task<object> FeesResultAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await FeesAsync(ct);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var source = organization.AppointmentTypes.AsQueryable();
        if (query.Term is string term)
            source = source.Where(t => t.Name.ToLower().Contains(term));
        source = (query.Sort, query.Desc) switch
        {
            ("amount", false) => source.OrderBy(t => t.Fees.Where(f => f.EffectiveFrom <= today).OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).Select(f => (decimal?)f.Amount).FirstOrDefault() ?? -1).ThenBy(t => t.Name),
            ("amount", true) => source.OrderByDescending(t => t.Fees.Where(f => f.EffectiveFrom <= today).OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).Select(f => (decimal?)f.Amount).FirstOrDefault() ?? -1).ThenBy(t => t.Name),
            ("from", false) => source.OrderBy(t => t.Fees.Where(f => f.EffectiveFrom <= today).OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).Select(f => (DateOnly?)f.EffectiveFrom).FirstOrDefault() ?? DateOnly.MinValue).ThenBy(t => t.Name),
            ("from", true) => source.OrderByDescending(t => t.Fees.Where(f => f.EffectiveFrom <= today).OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).Select(f => (DateOnly?)f.EffectiveFrom).FirstOrDefault() ?? DateOnly.MinValue).ThenBy(t => t.Name),
            ("name", true) => source.OrderByDescending(t => t.Name),
            _ => source.OrderBy(t => t.Name)
        };
        var rows = source.Select(t => new FeeRowDto(
            t.Id,
            t.Name,
            t.Fees.Where(f => f.EffectiveFrom <= today).OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).Select(f => (decimal?)f.Amount).FirstOrDefault(),
            t.Fees.Where(f => f.EffectiveFrom <= today).OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).Select(f => (DateOnly?)f.EffectiveFrom).FirstOrDefault()));
        return await Pages.Materialize(rows, query, ct);
    }

    public async Task<FeeDto> CreateFeeAsync(CreateFeeCommand command, CancellationToken ct)
    {
        var type = await organization.GetAppointmentTypeAsync(command.AppointmentTypeId, ct)
            ?? throw new NotFoundException("Tipo de turno no encontrado.");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var current = await store.GetCurrentFeeAsync(command.AppointmentTypeId, today, ct);
        var error = BillingRules.PriceChangeError(command.Amount, current?.Amount);
        if (error is not null)
            throw new BusinessRuleException(error);

        var amount = BillingRules.NormalizePrice(command.Amount);
        Fee fee;
        if (current is not null && current.EffectiveFrom == today)
        {
            current.Amount = amount;
            await store.RemoveFeesEffectiveFromAsync(command.AppointmentTypeId, today, current.Id, ct);
            await store.SaveAsync(ct);
            fee = current;
        }
        else
        {
            await store.RemoveFeesEffectiveFromAsync(command.AppointmentTypeId, today, null, ct);
            fee = await store.AddFeeAsync(new Fee
            {
                Id = Guid.NewGuid(),
                AppointmentTypeId = command.AppointmentTypeId,
                Amount = amount,
                EffectiveFrom = today
            }, ct);
        }

        await audit.RecordAsync(currentUser.Id, "arancel", "Arancel", fee.Id.ToString(), fee.Amount.ToString("0.00"), ct);
        return new FeeDto(fee.Id, fee.AppointmentTypeId, type.Name, fee.Amount, fee.EffectiveFrom);
    }

    public async Task<IReadOnlyList<InvoiceDto>> InvoicesAsync(CancellationToken ct) =>
        await store.Invoices.OrderByDescending(i => i.CreatedAt).ToInvoiceDtos().ToListAsync(ct);

    public async Task<object> InvoicesResultAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await InvoicesAsync(ct);
        var source = store.Invoices.AsQueryable();
        if (query.Term is string term)
            source = source.Where(i =>
                (i.Client.User.FirstName + " " + i.Client.User.LastName).ToLower().Contains(term) ||
                i.Total.ToString().Contains(term) ||
                (i.Status == InvoiceStatus.Voided ? "anulado" : i.Status == InvoiceStatus.Paid ? "pagado" : "pendiente").Contains(term) ||
                (i.PaymentMethod == PaymentMethod.Card ? "tarjeta" : i.PaymentMethod == PaymentMethod.Transfer ? "transferencia" : i.PaymentMethod == PaymentMethod.Cash ? "efectivo" : "").Contains(term));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("total", false) => source.OrderBy(i => i.Total).ThenBy(i => i.Client.User.LastName),
            ("total", true) => source.OrderByDescending(i => i.Total).ThenBy(i => i.Client.User.LastName),
            ("status", false) => source.OrderBy(i => i.Status == InvoiceStatus.Voided ? "Anulado" : i.Status == InvoiceStatus.Paid ? "Pagado" : "Pendiente").ThenBy(i => i.Client.User.LastName),
            ("status", true) => source.OrderByDescending(i => i.Status == InvoiceStatus.Voided ? "Anulado" : i.Status == InvoiceStatus.Paid ? "Pagado" : "Pendiente").ThenBy(i => i.Client.User.LastName),
            ("payment", false) => source.OrderBy(i => i.PaymentMethod == PaymentMethod.Card ? "Tarjeta" : i.PaymentMethod == PaymentMethod.Transfer ? "Transferencia" : i.PaymentMethod == PaymentMethod.Cash ? "Efectivo" : "").ThenBy(i => i.Client.User.LastName),
            ("payment", true) => source.OrderByDescending(i => i.PaymentMethod == PaymentMethod.Card ? "Tarjeta" : i.PaymentMethod == PaymentMethod.Transfer ? "Transferencia" : i.PaymentMethod == PaymentMethod.Cash ? "Efectivo" : "").ThenBy(i => i.Client.User.LastName),
            ("patient", true) => source.OrderByDescending(i => i.Client.User.LastName).ThenByDescending(i => i.Client.User.FirstName),
            _ => source.OrderBy(i => i.Client.User.LastName).ThenBy(i => i.Client.User.FirstName)
        };
        return await Pages.Materialize(ordered.ToInvoiceDtos(), query, ct);
    }

    public async Task<InvoiceDto> PayAsync(Guid id, PayInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (!BillingRules.CanPay(invoice.Status))
            throw new BusinessRuleException("Ese comprobante no admite un pago.");
        var amounts = await store.Invoices.Where(i => i.Id == invoice.Id).SelectMany(i => i.Items).Select(i => i.Amount).ToListAsync(ct);
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaymentMethod = command.Method;
        invoice.PaidAt = clock.GetUtcNow();
        invoice.Total = BillingRules.Total(amounts);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "pago", "Comprobante", invoice.Id.ToString(), command.Method.ToString(), ct);
        return await store.Invoices.Where(i => i.Id == invoice.Id).ToInvoiceDtos().FirstAsync(ct);
    }
}
