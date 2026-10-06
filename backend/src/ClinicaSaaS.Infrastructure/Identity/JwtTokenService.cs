using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicaSaaS.Application;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaSaaS.Infrastructure;

public class JwtTokenService(JwtOptions jwt) : ITokenService
{
    public string CreatePlatformToken(Guid userId, string email, string name) =>
        Create(userId, email, name, [AppRoles.SuperAdmin], null, false);

    public string CreateTenantToken(Guid userId, string email, string name, string slug, IReadOnlyList<string> roles, bool mustChangePassword) =>
        Create(userId, email, name, roles, slug, mustChangePassword);

    private string Create(Guid userId, string email, string name, IReadOnlyList<string> roles, string? slug, bool mustChangePassword)
    {
        var distinctRoles = roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.Ordinal).ToArray();
        if (distinctRoles.Length == 0)
            throw new InvalidOperationException("El token necesita al menos un rol.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, name),
            new(AuthClaims.LoggedUserId, userId.ToString()),
            new(AuthClaims.MustChangePassword, mustChangePassword ? "true" : "false")
        };
        if (slug is not null)
            claims.Add(new Claim(AuthClaims.TenantSlug, slug));
        foreach (var role in distinctRoles)
            claims.Add(new Claim(AuthClaims.Role, role));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            NotBefore = now,
            IssuedAt = now,
            Expires = now.AddMinutes(jwt.ExpiresMinutes),
            SigningCredentials = credentials
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
