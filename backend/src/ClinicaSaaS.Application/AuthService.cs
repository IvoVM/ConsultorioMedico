using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class AuthService(
    ITenantUserStore users,
    IPatientStore patients,
    ITokenService tokens,
    ICurrentTenant tenant,
    IRefreshSessionStore sessions,
    IValidator<RegisterPatientCommand> registrationValidator,
    IAuditStore audit)
{
    public async Task<IssuedSession> LoginTenantAsync(LoginCommand command, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(command.Email.Trim(), ct)
            ?? throw new BusinessRuleException("Credenciales inválidas.");
        if (!await users.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new BusinessRuleException("Credenciales inválidas.");
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedSession> RegisterPatientAsync(RegisterPatientCommand command, CancellationToken ct)
    {
        var result = await registrationValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        if (await users.FindByEmailAsync(command.Email.Trim(), ct) is not null)
            throw new ConflictException("Ese email ya está registrado.");

        var user = await users.CreateAsync(
            command.Email.Trim(),
            command.Password,
            command.FirstName.Trim(),
            command.LastName.Trim(),
            TenantRole.Patient,
            null,
            null,
            false,
            ct);
        await patients.AddAsync(new Patient
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentNumber = command.DocumentNumber.Trim(),
            BirthDate = command.BirthDate,
            Phone = command.Phone.Trim()
        }, ct);
        await audit.RecordAsync(user.Id, "registro", "Paciente", user.Id.ToString(), command.Email, ct);
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedSession> RefreshAsync(string? rawToken, CancellationToken ct)
    {
        var grant = await sessions.RotateAsync(rawToken, ct)
            ?? throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        var user = await users.FindByIdAsync(grant.UserId, ct)
            ?? throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return new IssuedSession(Access(user, name), grant.RawToken, grant.Expires);
    }

    public Task LogoutAsync(string? rawToken, CancellationToken ct) => sessions.RevokeAsync(rawToken, ct);

    private async Task<IssuedSession> IssueAsync(TenantAccount user, CancellationToken ct)
    {
        var grant = await sessions.IssueAsync(user.Id, ct);
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return new IssuedSession(Access(user, name), grant.RawToken, grant.Expires);
    }

    private TokenDto Access(TenantAccount user, string name) => new(
        tokens.CreateTenantToken(user.Id, user.Email, name, tenant.Slug, [user.Role.ToString()], user.MustChangePassword),
        user.Email,
        name,
        user.Role.ToString(),
        tenant.Slug,
        user.MustChangePassword);
}
