namespace ClinicaSaaS.Domain;

public class AuditEntry
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
