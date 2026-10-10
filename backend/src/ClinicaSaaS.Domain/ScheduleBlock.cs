using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class ScheduleBlock
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public Guid LocationId { get; set; }
    public Guid AppointmentTypeId { get; set; }


    public virtual TenantUser Professional { get; set; } = null!;
    public virtual Location Location { get; set; } = null!;
    public virtual AppointmentType AppointmentType { get; set; } = null!;
}
