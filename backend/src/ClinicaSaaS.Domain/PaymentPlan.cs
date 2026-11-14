namespace ClinicaSaaS.Domain;

// The balance of an invoice split into monthly installments. Payments made after the plan was set up
// cover the installments in order, so the plan only stores what each one is and when it falls due.
public class PaymentPlan
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public decimal Amount { get; set; }
    // Payments the invoice already had when the plan was made; the installments start after them.
    public decimal PaidBefore { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }


    public virtual Invoice? Invoice { get; set; }
    public virtual List<PlanInstallment> Installments { get; set; } = [];
}

public class PlanInstallment
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int Number { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }


    public virtual PaymentPlan? Plan { get; set; }
}
