using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class Employee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public virtual TenantUser User { get; set; } = null!;
    public string? LicenseNumber { get; set; }
    public Guid? SpecialtyId { get; set; }
    public virtual Specialty? Specialty { get; set; }
}
