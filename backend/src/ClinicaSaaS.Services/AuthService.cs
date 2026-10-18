using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.Utilities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class AuthService(
    ITenantUserStore users,
    IClientStore clients,
    IBookingRequestStore requests,
    ITokenService tokens,
    ICurrentTenant tenant,
    IRefreshSessionStore sessions,
    IValidator<RegisterPatientCommand> registrationValidator,
    IAuditStore audit,
    IEmployeeStore employees)
{
    public async Task<IssuedSession> LoginTenantAsync(LoginCommand command, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(command.Email.Trim(), ct)
            ?? throw new BusinessRuleException("Credenciales inválidas.");
        if (!await users.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new BusinessRuleException("Credenciales inválidas.");
        if (await IsInactiveEmployeeAsync(user.Id, ct))
            throw new BusinessRuleException("Esta cuenta está dada de baja.");
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedSession> LoginPatientAsync(LoginPatientCommand command, CancellationToken ct)
    {
        var document = DocumentRules.Normalize(command.DocumentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        var match = await clients.Clients
            .Where(client => client.DocumentNumber == document || client.DocumentNumber.ToLower() == document.ToLower())
            .Select(client => new { client.UserId, client.User.Email })
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("No hay una cuenta con ese documento. Creala con el mismo número para ver tus solicitudes.");
        if (IsPlaceholder(match.Email))
            throw new BusinessRuleException("Todavía no creaste la cuenta. Registrate con este documento para ver tus solicitudes.");
        var user = await users.FindByIdAsync(match.UserId, ct)
            ?? throw new BusinessRuleException("No hay una cuenta con ese documento. Creala con el mismo número para ver tus solicitudes.");
        if (user.Role != TenantRole.Patient)
            throw new BusinessRuleException("El documento o la contraseña no coinciden.");
        if (!await users.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new BusinessRuleException("El documento o la contraseña no coinciden.");
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedSession> RegisterPatientAsync(RegisterPatientCommand command, CancellationToken ct)
    {
        var result = await registrationValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        var document = DocumentRules.Normalize(command.DocumentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        var email = command.Email.Trim();
        var existing = await clients.Clients
            .Where(client => client.DocumentNumber == document || client.DocumentNumber.ToLower() == document.ToLower())
            .Select(client => new { client.Id, client.UserId, client.User.Email })
            .FirstOrDefaultAsync(ct);
        if (existing is not null && !IsPlaceholder(existing.Email))
            throw new ConflictException("Ya hay una cuenta con ese documento. Entrá con el documento y la contraseña.");
        if (await users.FindByEmailAsync(email, ct) is { } owner && (existing is null || owner.Id != existing.UserId))
            throw new ConflictException("Ese email ya está registrado.");

        TenantAccount user;
        Guid clientId;
        if (existing is not null)
        {
            await users.UpdateProfileAsync(existing.UserId, command.FirstName.Trim(), command.LastName.Trim(), email, TenantRole.Patient, ct);
            await users.SetPasswordAsync(existing.UserId, command.Password, ct);
            var client = await clients.GetAsync(existing.Id, ct)
                ?? throw new NotFoundException("Paciente no encontrado.");
            client.DocumentNumber = document;
            client.BirthDate = command.BirthDate;
            client.Phone = command.Phone.Trim();
            await clients.SaveAsync(ct);
            user = await users.FindByIdAsync(existing.UserId, ct)
                ?? throw new NotFoundException("Usuario no encontrado.");
            clientId = existing.Id;
        }
        else
        {
            user = await users.CreateAsync(
                email,
                command.Password,
                command.FirstName.Trim(),
                command.LastName.Trim(),
                TenantRole.Patient,
                false,
                ct);
            var client = await clients.AddAsync(new Client
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DocumentNumber = document,
                BirthDate = command.BirthDate,
                Phone = command.Phone.Trim()
            }, ct);
            clientId = client.Id;
        }

        await requests.LinkDocumentAsync(document, clientId, ct);
        await audit.RecordAsync(user.Id, "registro", "Paciente", user.Id.ToString(), email, ct);
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedSession> RefreshAsync(string? rawToken, CancellationToken ct)
    {
        var grant = await sessions.RotateAsync(rawToken, ct)
            ?? throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        var user = await users.FindByIdAsync(grant.UserId, ct)
            ?? throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        if (await IsInactiveEmployeeAsync(user.Id, ct))
            throw new UnauthorizedException("Esta cuenta está dada de baja.");
        return new IssuedSession(Access(user, user.Name), grant.RawToken, grant.Expires);
    }

    public Task LogoutAsync(string? rawToken, CancellationToken ct) => sessions.RevokeAsync(rawToken, ct);

    private static bool IsPlaceholder(string? email) =>
        email is not null && email.StartsWith("solicitud.", StringComparison.OrdinalIgnoreCase) && email.EndsWith("@pacientes.local", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsInactiveEmployeeAsync(Guid userId, CancellationToken ct)
    {
        var active = await employees.Employees.Where(e => e.UserId == userId).Select(e => (bool?)e.IsActive).FirstOrDefaultAsync(ct);
        return active == false;
    }

    private async Task<IssuedSession> IssueAsync(TenantAccount user, CancellationToken ct)
    {
        var grant = await sessions.IssueAsync(user.Id, ct);
        return new IssuedSession(Access(user, user.Name), grant.RawToken, grant.Expires);
    }

    private TokenDto Access(TenantAccount user, string name) => new(
        tokens.CreateTenantToken(user.Id, user.Email, name, tenant.Slug, [user.Role.ToString()], user.MustChangePassword),
        user.Email,
        name,
        user.Role.ToString(),
        tenant.Slug,
        user.MustChangePassword);
}
