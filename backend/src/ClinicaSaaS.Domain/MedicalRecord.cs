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
    // The emergency contact is someone else's data: the patient confirmed that person agreed to be called. Null while
    // there is no contact, or for a contact loaded before this was asked.
    public DateTimeOffset? EmergencyContactAgreedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }


    public virtual Client Client { get; set; } = null!;
}
