namespace ClinicaSaaS.Domain;

// One payment toward the patient's share of an invoice; several add up when the patient pays in parts.
public class InvoicePayment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTimeOffset PaidAt { get; set; }


    public virtual Invoice? Invoice { get; set; }
}
