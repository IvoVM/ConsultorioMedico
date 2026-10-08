namespace ClinicaSaaS.Domain;

public class Encounter
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? Note { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? WeightKg { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<EncounterDiagnosis> Diagnoses { get; set; } = [];
}
