using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using Microsoft.AspNetCore.Identity;

namespace ClinicaSaaS.Infrastructure.Identity;

public class PlatformUser : IdentityUser<Guid>
{
    public string Name { get; set; } = "";
}

public class TenantUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? LicenseNumber { get; set; }
    public Guid? SpecialtyId { get; set; }
    public TenantRole Role { get; set; }
    public bool MustChangePassword { get; set; }
}

public sealed class CurrentTenant : ICurrentTenant
{
    public bool IsResolved { get; private set; }
    public string Slug { get; private set; } = "";
    public string? ConnectionString { get; private set; }
    public TenantStatus Status { get; private set; }

    public void Resolve(string slug, string connectionString, TenantStatus status)
    {
        Slug = slug;
        ConnectionString = connectionString;
        Status = status;
        IsResolved = true;
    }
}
