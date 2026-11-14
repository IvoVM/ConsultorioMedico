namespace ClinicaSaaS.Domain;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ClientId { get; set; }
    // Sum of the items minus the discount.
    public decimal Total { get; set; }
    public decimal Discount { get; set; }
    // The share billed to the health insurer; the patient owes the rest of the total.
    public string? Insurer { get; set; }
    public decimal InsurerAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    // When the patient's share was fully paid.
    public DateTimeOffset? PaidAt { get; set; }

    // Filled once the tax authority authorizes the invoice; from then on its items cannot change.
    public FiscalVoucherType? VoucherType { get; set; }
    public int? PointOfSale { get; set; }
    public int? Number { get; set; }
    public string? Cae { get; set; }
    public DateOnly? CaeDueDate { get; set; }
    public DateTimeOffset? AuthorizedAt { get; set; }
    // True when the CAE came from the simulator instead of the real tax authority.
    public bool FiscalSimulated { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    // An authorized invoice is cancelled with a credit note, numbered and authorized on its own.
    public FiscalVoucherType? CreditNoteType { get; set; }
    public int? CreditNoteNumber { get; set; }
    public string? CreditNoteCae { get; set; }


    public virtual Appointment Appointment { get; set; } = null!;
    public virtual Client Client { get; set; } = null!;
    public virtual List<InvoiceItem> Items { get; set; } = [];
    public virtual List<InvoicePayment> Payments { get; set; } = [];
    public virtual List<PaymentPlan> Plans { get; set; } = [];
}
