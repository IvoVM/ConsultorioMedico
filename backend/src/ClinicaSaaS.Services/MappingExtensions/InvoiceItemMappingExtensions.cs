using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class InvoiceItemMappingExtensions
{
    public static InvoiceItemDto MapToInvoiceItemDto(this InvoiceItem item) =>
        new(item.Description, item.Quantity, item.UnitPrice, item.Amount);

    public static InvoicePaymentDto MapToInvoicePaymentDto(this InvoicePayment payment) =>
        new(payment.Id, payment.Amount, payment.Method, payment.PaidAt, payment.Receipt?.CardBrand, payment.Receipt?.CardInstallments);
}
