using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class BookingRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public Guid SpecialtyId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public Guid? InsurerId { get; set; }
    public string? MemberNumber { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public BookingRequestStatus Status { get; set; } = BookingRequestStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTimeOffset? PreviousStart { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public virtual Specialty Specialty { get; set; } = null!;
    public virtual TenantUser Professional { get; set; } = null!;
    public virtual Location Location { get; set; } = null!;
    public virtual AppointmentType AppointmentType { get; set; } = null!;
    public virtual Insurer? Insurer { get; set; }
    public virtual Client? Client { get; set; }
    public virtual Appointment? Appointment { get; set; }
}
