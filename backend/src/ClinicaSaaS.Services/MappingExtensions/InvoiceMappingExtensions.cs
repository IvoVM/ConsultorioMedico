using System.Linq.Expressions;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class InvoiceMappingExtensions
{
    public static InvoiceDto MapToInvoiceDto(this Invoice invoice)
    {
        var paid = BillingRules.Total(invoice.Payments.Select(p => p.Amount));
        return new(
            invoice.Id,
            invoice.AppointmentId,
            invoice.ClientId,
            (invoice.Client.FirstName + " " + invoice.Client.LastName).Trim(),
            invoice.Client.DocumentNumber,
            invoice.Appointment.Professional.Name,
            invoice.Appointment.Start,
            invoice.Total + invoice.Discount,
            invoice.Discount,
            invoice.Total,
            invoice.Insurer,
            invoice.InsurerAmount,
            BillingRules.PatientAmount(invoice.Total, invoice.InsurerAmount),
            paid,
            invoice.Status == InvoiceStatus.Voided ? 0 : BillingRules.Balance(invoice.Total, invoice.InsurerAmount, paid),
            invoice.Status,
            invoice.CreatedAt,
            invoice.PaidAt,
            invoice.AuthorizedAt,
            invoice.VoucherType,
            invoice.PointOfSale,
            invoice.Number,
            invoice.Cae,
            invoice.CaeDueDate,
            invoice.FiscalSimulated,
            invoice.VoidedAt,
            invoice.VoidReason,
            invoice.CreditNoteType,
            invoice.CreditNoteNumber,
            invoice.CreditNoteCae,
            BillingRules.CanEdit(invoice),
            invoice.Items.Select(item => item.MapToInvoiceItemDto()).ToList(),
            invoice.Payments.OrderBy(p => p.PaidAt).Select(p => p.MapToInvoicePaymentDto()).ToList());
    }

    public static Expression<Func<Invoice, Invoice>> MapForInvoiceDto() =>
        invoice => new Invoice
        {
            Id = invoice.Id,
            AppointmentId = invoice.AppointmentId,
            ClientId = invoice.ClientId,
            Total = invoice.Total,
            Discount = invoice.Discount,
            Insurer = invoice.Insurer,
            InsurerAmount = invoice.InsurerAmount,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt,
            PaidAt = invoice.PaidAt,
            VoucherType = invoice.VoucherType,
            PointOfSale = invoice.PointOfSale,
            Number = invoice.Number,
            Cae = invoice.Cae,
            CaeDueDate = invoice.CaeDueDate,
            FiscalSimulated = invoice.FiscalSimulated,
            AuthorizedAt = invoice.AuthorizedAt,
            VoidedAt = invoice.VoidedAt,
            VoidReason = invoice.VoidReason,
            CreditNoteType = invoice.CreditNoteType,
            CreditNoteNumber = invoice.CreditNoteNumber,
            CreditNoteCae = invoice.CreditNoteCae,
            Client = new Client
            {
                FirstName = invoice.Client.FirstName,
                LastName = invoice.Client.LastName,
                DocumentNumber = invoice.Client.DocumentNumber
            },
            Appointment = new Appointment
            {
                Start = invoice.Appointment.Start,
                Professional = new TenantUser
                {
                    FirstName = invoice.Appointment.Professional.FirstName,
                    LastName = invoice.Appointment.Professional.LastName
                }
            },
            Items = invoice.Items.OrderBy(item => item.Position).Select(item => new InvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount
            }).ToList(),
            Payments = invoice.Payments.Select(payment => new InvoicePayment
            {
                Id = payment.Id,
                Amount = payment.Amount,
                Method = payment.Method,
                PaidAt = payment.PaidAt
            }).ToList()
        };
}
