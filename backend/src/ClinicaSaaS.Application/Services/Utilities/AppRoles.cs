using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application.Services.Utilities;

public static class AppRoles
{
    public const string TenantAdmin = nameof(TenantRole.TenantAdmin);
    public const string Doctor = nameof(TenantRole.Doctor);
    public const string Secretary = nameof(TenantRole.Secretary);
    public const string Patient = nameof(TenantRole.Patient);
}
