using System.Security.Cryptography;
using System.Text;
using ClinicaSaaS.Application;
using ClinicaSaaS.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaSaaS.Infrastructure.Persistence;

public class RefreshSessionStore(TenantDbContext db, TimeProvider clock, JwtOptions jwt) : IRefreshSessionStore
{
    public async Task<RefreshGrant> IssueAsync(Guid userId, CancellationToken ct)
    {
        var grant = NewGrant(userId);
        db.RefreshSessions.Add(ToSession(grant));
        await db.SaveChangesAsync(ct);
        return grant;
    }

    public async Task<RefreshGrant?> RotateAsync(string? rawToken, CancellationToken ct)
    {
        if (!TryParse(rawToken, out var id, out var secret))
            return null;

        var session = await db.RefreshSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null)
            return null;

        var now = clock.GetUtcNow();
        if (session.RevokedAt is not null || !HashEquals(session.TokenHash, secret))
        {
            await RevokeUserAsync(session.UserId, now, ct);
            return null;
        }

        if (session.ExpiresAt <= now)
            return null;

        var grant = NewGrant(session.UserId);
        if (!TryParse(grant.RawToken, out var nextId, out _))
            throw new InvalidOperationException("No se pudo emitir la sesión de refresco.");
        session.RevokedAt = now;
        session.ReplacedById = nextId;
        db.RefreshSessions.Add(ToSession(grant));
        await db.SaveChangesAsync(ct);
        return grant;
    }

    public async Task RevokeAsync(string? rawToken, CancellationToken ct)
    {
        if (!TryParse(rawToken, out var id, out _))
            return;
        var session = await db.RefreshSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null || session.RevokedAt is not null)
            return;
        session.RevokedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    private RefreshGrant NewGrant(Guid userId)
    {
        var secret = RandomNumberGenerator.GetBytes(32);
        var id = Guid.NewGuid();
        var expires = clock.GetUtcNow().AddDays(jwt.RefreshDays);
        return new RefreshGrant(userId, $"{id:N}.{Base64UrlEncoder.Encode(secret)}", expires);
    }

    private RefreshSession ToSession(RefreshGrant grant)
    {
        TryParse(grant.RawToken, out var id, out var secret);
        return new RefreshSession
        {
            Id = id,
            UserId = grant.UserId,
            TokenHash = Hash(secret),
            ExpiresAt = grant.Expires,
            CreatedAt = clock.GetUtcNow()
        };
    }

    private async Task RevokeUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var active = await db.RefreshSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var session in active)
            session.RevokedAt = now;
        if (active.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    private static bool TryParse(string? raw, out Guid id, out byte[] secret)
    {
        id = Guid.Empty;
        secret = [];
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var dot = raw.IndexOf('.');
        if (dot <= 0 || dot == raw.Length - 1)
            return false;
        if (!Guid.TryParseExact(raw[..dot], "N", out id))
            return false;
        try
        {
            secret = Base64UrlEncoder.DecodeBytes(raw[(dot + 1)..]);
        }
        catch (FormatException)
        {
            return false;
        }

        return secret.Length > 0;
    }

    private static string Hash(byte[] secret) => Convert.ToHexString(SHA256.HashData(secret));

    private static bool HashEquals(string stored, byte[] secret)
    {
        var actual = Hash(secret);
        var left = Encoding.UTF8.GetBytes(stored);
        var right = Encoding.UTF8.GetBytes(actual);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
