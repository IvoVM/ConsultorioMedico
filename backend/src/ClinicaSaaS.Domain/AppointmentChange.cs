using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

// One decision on an appointment: booked, moved, admitted, cancelled, absent... with the slot it left the appointment in.
public class AppointmentChange : Auditable
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public Guid ProfessionalId { get; set; }


    public virtual Appointment Appointment { get; set; } = null!;
    public virtual TenantUser Professional { get; set; } = null!;
}
