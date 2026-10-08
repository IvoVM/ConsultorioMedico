using ClinicaSaaS.Domain;
using Microsoft.AspNetCore.Identity;

namespace ClinicaSaaS.Infrastructure.Identity;

public class TenantUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public TenantRole Role { get; set; }
    public bool MustChangePassword { get; set; }

    public string Name => $"{FirstName} {LastName}".Trim();
}
