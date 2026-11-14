namespace ClinicaSaaS.Domain;

// The part of a receipt applied to one invoice. A receipt can cover several invoices, and an invoice
// can be paid by several receipts; what a receipt does not cover stays as the patient's credit.
public class InvoicePayment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ReceiptId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTimeOffset PaidAt { get; set; }


    public virtual Invoice? Invoice { get; set; }
    public virtual Receipt? Receipt { get; set; }
}
