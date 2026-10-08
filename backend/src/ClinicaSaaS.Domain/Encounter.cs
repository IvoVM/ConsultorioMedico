using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class Encounter
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public virtual Appointment Appointment { get; set; } = null!;
    public Guid ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;
    public Guid ProfessionalId { get; set; }
    public virtual TenantUser Professional { get; set; } = null!;
    public string? Note { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? WeightKg { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public virtual List<EncounterDiagnosis> Diagnoses { get; set; } = [];
}
