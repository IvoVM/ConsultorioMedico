namespace ClinicaSaaS.Application;

public static class AuthClaims
{
    public const string LoggedUserId = "_loggedUserId";
    public const string Role = "role";
    public const string TenantSlug = "tenant_slug";
    public const string MustChangePassword = "must_change_password";
}

public static class AppRoles
{
    public const string TenantAdmin = nameof(Domain.TenantRole.TenantAdmin);
    public const string Doctor = nameof(Domain.TenantRole.Doctor);
    public const string Secretary = nameof(Domain.TenantRole.Secretary);
    public const string Patient = nameof(Domain.TenantRole.Patient);
}
