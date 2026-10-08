using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class Client
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public virtual TenantUser User { get; set; } = null!;
    public virtual MedicalRecord? Record { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string Phone { get; set; } = "";
}
