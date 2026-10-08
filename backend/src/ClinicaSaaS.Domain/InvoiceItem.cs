namespace ClinicaSaaS.Domain;

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public virtual Invoice? Invoice { get; set; }
}
