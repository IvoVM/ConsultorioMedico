using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class ScheduleBlockout
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string Reason { get; set; } = "";


    public virtual TenantUser Professional { get; set; } = null!;
}
