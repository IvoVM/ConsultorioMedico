using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class InvoiceService(
    IBillingStore store,
    ICurrentUser currentUser,
    IAuditStore audit,
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

    public async Task<object> InvoicesResultAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await InvoicesAsync(ct);
        var source = store.Invoices.AsQueryable();
        if (query.Term is string term)
            source = source.Where(i =>
                (i.Client.FirstName + " " + i.Client.LastName).ToLower().Contains(term) ||
                i.Total.ToString().Contains(term) ||
                (i.Status == InvoiceStatus.Voided ? "anulado" : i.Status == InvoiceStatus.Paid ? "pagado" : "pendiente").Contains(term) ||
                (i.PaymentMethod == PaymentMethod.Card ? "tarjeta" : i.PaymentMethod == PaymentMethod.Transfer ? "transferencia" : i.PaymentMethod == PaymentMethod.Cash ? "efectivo" : "").Contains(term));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("total", false) => source.OrderBy(i => i.Total).ThenBy(i => i.Client.LastName),
            ("total", true) => source.OrderByDescending(i => i.Total).ThenBy(i => i.Client.LastName),
            ("status", false) => source.OrderBy(i => i.Status == InvoiceStatus.Voided ? "Anulado" : i.Status == InvoiceStatus.Paid ? "Pagado" : "Pendiente").ThenBy(i => i.Client.LastName),
            ("status", true) => source.OrderByDescending(i => i.Status == InvoiceStatus.Voided ? "Anulado" : i.Status == InvoiceStatus.Paid ? "Pagado" : "Pendiente").ThenBy(i => i.Client.LastName),
            ("payment", false) => source.OrderBy(i => i.PaymentMethod == PaymentMethod.Card ? "Tarjeta" : i.PaymentMethod == PaymentMethod.Transfer ? "Transferencia" : i.PaymentMethod == PaymentMethod.Cash ? "Efectivo" : "").ThenBy(i => i.Client.LastName),
            ("payment", true) => source.OrderByDescending(i => i.PaymentMethod == PaymentMethod.Card ? "Tarjeta" : i.PaymentMethod == PaymentMethod.Transfer ? "Transferencia" : i.PaymentMethod == PaymentMethod.Cash ? "Efectivo" : "").ThenBy(i => i.Client.LastName),
            ("patient", true) => source.OrderByDescending(i => i.Client.LastName).ThenByDescending(i => i.Client.FirstName),
            _ => source.OrderBy(i => i.Client.LastName).ThenBy(i => i.Client.FirstName)
        };
        var page = await Pages.Materialize(ordered.Select(InvoiceMappingExtensions.MapForInvoiceDto()), query, ct);
        return new PageDto<InvoiceDto>(page.Items.Select(invoice => invoice.MapToInvoiceDto()).ToList(), page.Total, page.Page, page.PageSize);
    }

    public async Task<InvoiceDto> PayAsync(Guid id, PayInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (!BillingRules.CanPay(invoice.Status))
            throw new BusinessRuleException("Ese comprobante ya no se puede cobrar.");
        var amounts = await store.Invoices.Where(i => i.Id == invoice.Id).SelectMany(i => i.Items).Select(i => i.Amount).ToListAsync(ct);
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaymentMethod = command.Method;
        invoice.PaidAt = clock.GetUtcNow();
        invoice.Total = BillingRules.Total(amounts);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "pago", "Comprobante", invoice.Id.ToString(), command.Method.ToString(), ct);
        var paid = await store.Invoices.Where(i => i.Id == invoice.Id).Select(InvoiceMappingExtensions.MapForInvoiceDto()).FirstAsync(ct);
        return paid.MapToInvoiceDto();
    }
}
