namespace ClinicaSaaS.Domain.QueryViews;

public sealed class ClientCard
{
    public Guid ClientId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string DocumentNumber { get; init; } = "";
    public DateOnly BirthDate { get; init; }
    public string Phone { get; init; } = "";
    public string? HealthInsurance { get; init; }
    public string? MemberNumber { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }
}
