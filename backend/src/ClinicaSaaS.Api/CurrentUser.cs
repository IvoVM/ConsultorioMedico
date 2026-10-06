using System.Security.Claims;
using ClinicaSaaS.Application;

namespace ClinicaSaaS.Api;

public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    public Guid? Id =>
        Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Role => http.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public string? TenantSlug => http.HttpContext?.User.FindFirstValue("tenant_slug");
}
