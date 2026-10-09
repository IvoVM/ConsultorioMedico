using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class BillingMappings
{
    public static IQueryable<FeeDto> ToFeeDtos(this IQueryable<Fee> query) =>
        query.Select(f => new FeeDto(f.Id, f.AppointmentTypeId, f.AppointmentType.Name, f.Amount, f.EffectiveFrom));

    public static IQueryable<InvoiceDto> ToInvoiceDtos(this IQueryable<Invoice> query) =>
        query.Select(i => new InvoiceDto(
            i.Id,
            i.AppointmentId,
            i.ClientId,
            (i.Client.User.FirstName + " " + i.Client.User.LastName).Trim(),
            i.Total,
            i.Status,
            i.PaymentMethod,
            i.CreatedAt,
            i.PaidAt,
            i.Items.Select(item => new InvoiceItemDto(item.Description, item.Amount)).ToList()));
}
