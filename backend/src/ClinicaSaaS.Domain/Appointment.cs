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
}
