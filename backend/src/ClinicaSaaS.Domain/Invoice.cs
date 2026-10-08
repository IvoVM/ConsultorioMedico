namespace ClinicaSaaS.Domain;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public virtual Appointment Appointment { get; set; } = null!;
    public Guid ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public virtual List<InvoiceItem> Items { get; set; } = [];
}
