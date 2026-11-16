namespace ClinicaSaaS.Domain;

// The patient's administrative file: coverage and who to call. Clinical facts (allergies, history, medication)
// are signed pages of the clinical record, written only by professionals.
public class MedicalRecord
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string? HealthInsurance { get; set; }
    public string? MemberNumber { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }


    public virtual Client Client { get; set; } = null!;
}
