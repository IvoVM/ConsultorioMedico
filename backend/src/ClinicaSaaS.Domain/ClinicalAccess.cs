namespace ClinicaSaaS.Domain;

// Who opened, copied or wrote a patient's record and when. The patient can read it; nobody can change it.
public class ClinicalAccess
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = "";
    public TenantRole? Role { get; set; }
    public ClinicalAccessAction Action { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset At { get; set; }
}
