using System.Linq.Expressions;
using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class InvoiceMappingExtensions
{
    public static InvoiceDto MapToInvoiceDto(this Invoice invoice) =>
        new(
            invoice.Id,
            invoice.AppointmentId,
            invoice.ClientId,
            (invoice.Client.FirstName + " " + invoice.Client.LastName).Trim(),
            invoice.Total,
            invoice.Status,
            invoice.PaymentMethod,
            invoice.CreatedAt,
            invoice.PaidAt,
            invoice.Items.Select(item => item.MapToInvoiceItemDto()).ToList());

    public static Expression<Func<Invoice, Invoice>> MapForInvoiceDto() =>
        invoice => new Invoice
        {
            Id = invoice.Id,
            AppointmentId = invoice.AppointmentId,
            ClientId = invoice.ClientId,
            Total = invoice.Total,
            Status = invoice.Status,
            PaymentMethod = invoice.PaymentMethod,
            CreatedAt = invoice.CreatedAt,
            PaidAt = invoice.PaidAt,
            Client = new Client
            {
                FirstName = invoice.Client.FirstName,
                LastName = invoice.Client.LastName
            },
            Items = invoice.Items.Select(item => new InvoiceItem
            {
                Description = item.Description,
                Amount = item.Amount
            }).ToList()
        };
}
