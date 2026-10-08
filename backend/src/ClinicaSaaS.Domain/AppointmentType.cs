namespace ClinicaSaaS.Domain;

public class AppointmentType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; }
    public Guid? SpecialtyId { get; set; }
}
