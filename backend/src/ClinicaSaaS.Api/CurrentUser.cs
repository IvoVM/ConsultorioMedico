using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClinicaSaaS.Application;
using ClinicaSaaS.Application.Services.Utilities;

namespace ClinicaSaaS.Api;

public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal? Principal => http.HttpContext?.User;

    public Guid? Id
    {
        get
        {
            var value = Principal?.FindFirstValue(AuthClaims.LoggedUserId)
                ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(AuthClaims.Role).Select(claim => claim.Value).Distinct().ToArray() ?? [];

    public string? Role => Roles.Count > 0 ? Roles[0] : null;

    public string? TenantSlug => Principal?.FindFirstValue(AuthClaims.TenantSlug);
}
