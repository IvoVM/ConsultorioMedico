using System.Linq.Expressions;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class InvoiceMappingExtensions
{
    // Today decides which installments are overdue; the credit is the patient's, read apart for each page.
    public static InvoiceDto MapToInvoiceDto(this Invoice invoice, DateOnly? today = null, decimal patientCredit = 0)
    {
        var paid = BillingRules.Total(invoice.Payments.Select(p => p.Amount));
        var plan = invoice.Plans.Where(p => p.CancelledAt == null).OrderByDescending(p => p.CreatedAt).FirstOrDefault();
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
            invoice.Payments.OrderBy(p => p.PaidAt).Select(p => p.MapToInvoicePaymentDto()).ToList(),
            plan is null || invoice.Status == InvoiceStatus.Voided ? null : plan.MapToPlanDto(paid, today ?? DateOnly.FromDateTime(DateTime.UtcNow)),
            invoice.Status == InvoiceStatus.Voided ? 0 : patientCredit);
    }

    public static PaymentPlanDto MapToPlanDto(this PaymentPlan plan, decimal paidOnInvoice, DateOnly today)
    {
        var installments = plan.Installments.OrderBy(i => i.Number).ToList();
        var progress = PlanRules.Progress(installments.Select(i => (i.DueDate, i.Amount)).ToList(), paidOnInvoice - plan.PaidBefore, today);
        return new PaymentPlanDto(
            plan.Id,
            plan.Amount,
            plan.CreatedAt,
            installments.Select((i, n) => new InstallmentDto(i.Number, i.DueDate, i.Amount, progress[n].Paid, progress[n].Status)).ToList());
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
                PaidAt = payment.PaidAt,
                Receipt = payment.Receipt == null ? null : new Receipt
                {
                    CardBrand = payment.Receipt.CardBrand,
                    CardInstallments = payment.Receipt.CardInstallments
                }
            }).ToList(),
            Plans = invoice.Plans.Where(plan => plan.CancelledAt == null).Select(plan => new PaymentPlan
            {
                Id = plan.Id,
                Amount = plan.Amount,
                PaidBefore = plan.PaidBefore,
                CreatedAt = plan.CreatedAt,
                Installments = plan.Installments.Select(i => new PlanInstallment { Number = i.Number, DueDate = i.DueDate, Amount = i.Amount }).ToList()
            }).ToList()
        };
}
