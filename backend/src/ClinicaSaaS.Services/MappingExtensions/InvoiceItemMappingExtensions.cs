using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class InvoiceItemMappingExtensions
{
    public static InvoiceItemDto MapToInvoiceItemDto(this InvoiceItem item) =>
        new(item.Description, item.Amount);
}
