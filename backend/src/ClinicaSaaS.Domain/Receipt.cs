namespace ClinicaSaaS.Domain;

// Money a patient handed over (or, with a negative amount, money given back), entered in one register.
// It is applied to invoices through InvoicePayment; whatever is not applied is the patient's credit.
public class Receipt
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? CashSessionId { get; set; }
    public string? Note { get; set; }

    // Card payments carry what the card processor settles them by.
    public CardBrand? CardBrand { get; set; }
    public int? CardInstallments { get; set; }
    public string? CardBatch { get; set; }
    public string? CardCoupon { get; set; }
    public Guid? SettlementLineId { get; set; }


    public virtual Client Client { get; set; } = null!;
    public virtual Location? Location { get; set; }
    public virtual CashSession? CashSession { get; set; }
    public virtual List<InvoicePayment> Allocations { get; set; } = [];
}
