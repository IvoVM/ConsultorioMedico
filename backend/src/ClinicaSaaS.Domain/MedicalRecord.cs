namespace ClinicaSaaS.Domain;

public class MedicalRecord
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? PersonalHistory { get; set; }
    public string? FamilyHistory { get; set; }
    public string? CurrentMedication { get; set; }
    public string? Habits { get; set; }
    public string? HealthInsurance { get; set; }
    public string? MemberNumber { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }


    public virtual Client Client { get; set; } = null!;
}
