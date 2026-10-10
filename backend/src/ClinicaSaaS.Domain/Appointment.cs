using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public string? VisitReason { get; set; }


    public virtual Client Client { get; set; } = null!;
    public virtual TenantUser Professional { get; set; } = null!;
    public virtual Location Location { get; set; } = null!;
    public virtual AppointmentType AppointmentType { get; set; } = null!;
    public virtual Encounter? Encounter { get; set; }
    public virtual Invoice? Invoice { get; set; }
}
