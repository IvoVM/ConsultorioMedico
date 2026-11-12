namespace ClinicaSaaS.Domain;

// One row per clinic database: what the admin can change from the settings screen.
public class ClinicSettings
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool BillingEnabled { get; set; } = true;
    public bool AutoInvoice { get; set; } = true;
}
