using System.Security.Claims;
using ClinicaSaaS.Application;

namespace ClinicaSaaS.Api;

public class UsuarioActual(IHttpContextAccessor http) : IUsuarioActual
{
    public Guid? Id =>
        Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Rol => http.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public string? TenantSlug => http.HttpContext?.User.FindFirstValue("tenant_slug");
}
