namespace ClinicaSaaS.Domain.QueryViews;

public sealed class BusyInterval
{
    public Guid ProfessionalId { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public AppointmentStatus? Status { get; init; }
}
