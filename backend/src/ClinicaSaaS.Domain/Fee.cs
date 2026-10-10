namespace ClinicaSaaS.Domain;

public class Fee
{
    public Guid Id { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly EffectiveFrom { get; set; }


    public virtual AppointmentType AppointmentType { get; set; } = null!;
}
