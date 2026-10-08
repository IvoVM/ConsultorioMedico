namespace ClinicaSaaS.Domain;

public class WaitlistEntry
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid? ProfessionalId { get; set; }
    public Guid LocationId { get; set; }
    public Guid SpecialtyId { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Notes { get; set; }
}
