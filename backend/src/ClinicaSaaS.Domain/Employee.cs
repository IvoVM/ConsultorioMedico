namespace ClinicaSaaS.Domain;

public class Employee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? LicenseNumber { get; set; }
    public Guid? SpecialtyId { get; set; }
}
