namespace ClinicaSaaS.Domain;

// One row per clinic database: what the admin can change from the settings screen.
public class ClinicSettings
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool BillingEnabled { get; set; } = true;
    public bool AutoInvoice { get; set; } = true;
    // Electronic invoicing: invoices can be authorized with the tax authority, which needs the clinic's tax data.
    public bool FiscalEnabled { get; set; }
    public string? TaxId { get; set; }
    public TaxCondition? TaxCondition { get; set; }
    public int? PointOfSale { get; set; }
    // Authorize each invoice as soon as it is issued, instead of with a separate step.
    public bool AutoAuthorize { get; set; }
}
