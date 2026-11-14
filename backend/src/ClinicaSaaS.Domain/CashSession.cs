namespace ClinicaSaaS.Domain;

// One register shift at a site: opened with the cash in the drawer, closed with the cash counted.
public class CashSession
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;
    public decimal OpeningCash { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public Guid? OpenedById { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedById { get; set; }
    // Cash the expected figure should match: opening plus cash in, minus cash out.
    public decimal? ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public string? Notes { get; set; }


    public virtual Location Location { get; set; } = null!;
    public virtual List<CashMovement> Movements { get; set; } = [];
}

public class CashMovement
{
    public Guid Id { get; set; }
    public Guid CashSessionId { get; set; }
    public CashMovementKind Kind { get; set; }
    public PaymentMethod Method { get; set; }
    // Always positive; the kind says whether it went in or out.
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public DateTimeOffset At { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? UserId { get; set; }


    public virtual CashSession? CashSession { get; set; }
}
