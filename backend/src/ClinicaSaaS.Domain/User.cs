using ClinicaSaaS.Domain;
using Microsoft.AspNetCore.Identity;

namespace ClinicaSaaS.Infrastructure.Identity;

public class TenantUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public TenantRole Role { get; set; }
    public bool MustChangePassword { get; set; }
    public string Name => $"{FirstName} {LastName}".Trim();


    public virtual Client? Client { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = [];
    public virtual ICollection<ScheduleBlockout> ScheduleBlockouts { get; set; } = [];
    public virtual ICollection<Appointment> Appointments { get; set; } = [];
    public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = [];
    public virtual ICollection<Encounter> Encounters { get; set; } = [];
    public virtual ICollection<Prescription> Prescriptions { get; set; } = [];
    public virtual ICollection<RefreshSession> RefreshSessions { get; set; } = [];
}
