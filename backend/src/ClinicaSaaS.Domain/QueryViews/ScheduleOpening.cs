namespace ClinicaSaaS.Domain.QueryViews;

public sealed class ScheduleOpening
{
    public Guid ProfessionalId { get; init; }
    public string ProfessionalFirstName { get; init; } = "";
    public string ProfessionalLastName { get; init; } = "";
    public Guid LocationId { get; init; }
    public Guid AppointmentTypeId { get; init; }
    public string AppointmentType { get; init; } = "";
    public int DurationMinutes { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}
