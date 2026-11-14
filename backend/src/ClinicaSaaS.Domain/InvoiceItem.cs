namespace ClinicaSaaS.Domain;

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    // Order the lines were written in.
    public int Position { get; set; }
    public string Description { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    // Quantity times unit price.
    public decimal Amount { get; set; }


    public virtual Invoice? Invoice { get; set; }
}
