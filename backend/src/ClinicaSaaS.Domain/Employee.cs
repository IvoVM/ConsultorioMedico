using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class Employee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? DocumentNumber { get; set; }
    public string? LicenseNumber { get; set; }
    public Guid? SpecialtyId { get; set; }
    public bool IsActive { get; set; } = true;
    // The handwritten signature that goes with their stamp on the pages they sign from now on.
    public Guid? SignatureId { get; set; }


    public virtual TenantUser User { get; set; } = null!;
    public virtual Specialty? Specialty { get; set; }
    public virtual ICollection<Insurer> Insurers { get; set; } = [];
}
