namespace ClinicaSaaS.Domain;

public class Fee
{
    public Guid Id { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public string? Payer { get; set; }
    public decimal Amount { get; set; }
    // For an insurer's price, what the patient pays on top of it (coseguro).
    public decimal? Copay { get; set; }
    public DateOnly EffectiveFrom { get; set; }


    public virtual AppointmentType AppointmentType { get; set; } = null!;
}
