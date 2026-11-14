namespace ClinicaSaaS.Domain;

// What a card processor paid the clinic for a brand over some days, with each charge and its fee.
public class CardSettlement
{
    public Guid Id { get; set; }
    public CardBrand Brand { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
    // True when the lines came from the simulator instead of the processor.
    public bool Simulated { get; set; }


    public virtual List<CardSettlementLine> Lines { get; set; } = [];
}

public class CardSettlementLine
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public string Batch { get; set; } = "";
    public string Coupon { get; set; } = "";
    public int Installments { get; set; }
    public DateOnly ChargedOn { get; set; }
    public DateOnly PaidOn { get; set; }
    public decimal Gross { get; set; }
    public decimal Fee { get; set; }
    public decimal Net { get; set; }
    public Guid? ReceiptId { get; set; }


    public virtual CardSettlement? Settlement { get; set; }
}
