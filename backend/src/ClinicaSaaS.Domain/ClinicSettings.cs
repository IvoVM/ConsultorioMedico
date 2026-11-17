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
    // How many overbooks each professional may get per day: null means no limit, 0 means none at all.
    public int? MaxOverbooksPerDay { get; set; }
    // Where patients ask to see, fix or delete their data (an email, a phone, the desk); the privacy notice shows it.
    public string? PrivacyContact { get; set; }
}
