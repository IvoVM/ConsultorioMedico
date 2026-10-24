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
    IEmployeeStore employees,
    ICurrentUser currentUser)
{
    public async Task<IssuedSession> LoginTenantAsync(LoginCommand command, CancellationToken ct)
    {
        var document = DocumentRules.Normalize(command.DocumentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");

        var employee = await employees.Employees
            .Where(e => e.DocumentNumber == document || (e.DocumentNumber != null && e.DocumentNumber.ToLower() == document.ToLower()))
            .Select(e => new { e.UserId, e.IsActive })
            .FirstOrDefaultAsync(ct);

        Guid userId;
        if (employee is not null)
        {
            if (!employee.IsActive)
                throw new BusinessRuleException("Esta cuenta está dada de baja.");
            userId = employee.UserId;
        }
        else
        {
            var client = await clients.Clients
                .Where(c => c.DocumentNumber == document || c.DocumentNumber.ToLower() == document.ToLower())
                .Select(c => new { c.UserId, Email = c.User != null ? c.User.Email : null })
                .FirstOrDefaultAsync(ct)
                ?? throw new BusinessRuleException("El documento o la contraseña no coinciden.");
            if (client.UserId is not Guid patientUser || IsPlaceholder(client.Email))
                throw new BusinessRuleException("Todavía no creaste la cuenta. Registrate con este documento para ver tus solicitudes.");
            userId = patientUser;
        }

        var user = await users.FindByIdAsync(userId, ct)
            ?? throw new BusinessRuleException("El documento o la contraseña no coinciden.");
        if (!await users.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new BusinessRuleException("El documento o la contraseña no coinciden.");
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedSession> LoginPatientAsync(LoginPatientCommand command, CancellationToken ct)
    {
        var document = DocumentRules.Normalize(command.DocumentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        var match = await clients.Clients
            .Where(client => client.DocumentNumber == document || client.DocumentNumber.ToLower() == document.ToLower())
            .Select(client => new { client.UserId, Email = client.User != null ? client.User.Email : null })
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("No hay una cuenta con ese documento. Creala con el mismo número para ver tus solicitudes.");
        if (match.UserId is not Guid patientUser || IsPlaceholder(match.Email))
            throw new BusinessRuleException("Todavía no creaste la cuenta. Registrate con este documento para ver tus solicitudes.");
        var user = await users.FindByIdAsync(patientUser, ct)
            ?? throw new BusinessRuleException("No hay una cuenta con ese documento. Creala con el mismo número para ver tus solicitudes.");
        if (user.Role != TenantRole.Patient)
            throw new BusinessRuleException("El documento o la contraseña no coinciden.");
        if (!await users.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new BusinessRuleException("El documento o la contraseña no coinciden.");
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedSession> LoginStaffAsync(LoginStaffCommand command, CancellationToken ct)
    {
        var document = DocumentRules.Normalize(command.DocumentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        var match = await employees.Employees
            .Where(employee => employee.DocumentNumber == document || (employee.DocumentNumber != null && employee.DocumentNumber.ToLower() == document.ToLower()))
            .Select(employee => new { employee.UserId, employee.IsActive })
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("No hay un empleado con ese documento.");
        if (!match.IsActive)
            throw new BusinessRuleException("Esta cuenta está dada de baja.");
        var user = await users.FindByIdAsync(match.UserId, ct)
            ?? throw new BusinessRuleException("No hay un empleado con ese documento.");
        if (!await users.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new BusinessRuleException("El documento o la contraseña no coinciden.");
        return await IssueAsync(user, ct);
    }

    public async Task<PatientProfileDto> ProfileAsync(CancellationToken ct)
    {
        var client = await OwnClientAsync(ct);
        return new PatientProfileDto(client.FirstName, client.LastName, client.DocumentNumber, client.Phone);
    }

    public async Task<PatientProfileDto> UpdateProfileAsync(UpdatePatientProfileCommand command, CancellationToken ct)
    {
        var first = command.FirstName.Trim();
        var last = command.LastName.Trim();
        if (first.Length is < 1 or > 80 || last.Length is < 1 or > 80)
            throw new BusinessRuleException("Escribí tu nombre y tu apellido.");
        var client = await OwnClientAsync(ct);
        client.FirstName = first;
        client.LastName = last;
        client.Phone = PhoneRules.Require(command.Phone);
        await clients.SaveAsync(ct);
        await users.UpdateNameAsync(client.UserId!.Value, first, last, ct);
        return new PatientProfileDto(client.FirstName, client.LastName, client.DocumentNumber, client.Phone);
    }

    private async Task<Client> OwnClientAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        return await clients.GetByUserAsync(userId, ct)
            ?? throw new NotFoundException("No hay una ficha para esta cuenta.");
    }

    public async Task<IssuedSession> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken ct)
    {
        if (currentUser.Id is not Guid id)
            throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        var password = command.Password ?? "";
        if (PasswordRules.Issue(password) is string issue)
            throw new BusinessRuleException(issue);
        await users.SetPasswordAsync(id, password, ct);
        var user = await users.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");
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
        var email = AccountEmail.ForPatient(document);
        var existing = await clients.Clients
            .Where(client => client.DocumentNumber == document || client.DocumentNumber.ToLower() == document.ToLower())
            .Select(client => new { client.Id, client.UserId, Email = client.User != null ? client.User.Email : null })
            .FirstOrDefaultAsync(ct);
        if (existing is not null && existing.UserId is Guid && !IsPlaceholder(existing.Email))
            throw new ConflictException("Ya hay una cuenta con ese documento. Entrá con el documento y la contraseña.");
        if (await users.FindByEmailAsync(email, ct) is { } owner && (existing?.UserId is null || owner.Id != existing.UserId))
            throw new ConflictException("Ya hay una cuenta con ese documento. Entrá con el documento y la contraseña.");

        var firstName = command.FirstName.Trim();
        var lastName = command.LastName.Trim();
        var phone = PhoneRules.Require(command.Phone);
        TenantAccount user;
        Guid clientId;
        if (existing is not null && existing.UserId is Guid linkedUser)
        {
            await users.UpdateProfileAsync(linkedUser, firstName, lastName, email, TenantRole.Patient, ct);
            await users.SetPasswordAsync(linkedUser, command.Password, ct);
            var client = await clients.GetAsync(existing.Id, ct)
                ?? throw new NotFoundException("Paciente no encontrado.");
            client.FirstName = firstName;
            client.LastName = lastName;
            client.DocumentNumber = document;
            client.Phone = phone;
            await clients.SaveAsync(ct);
            user = await users.FindByIdAsync(linkedUser, ct)
                ?? throw new NotFoundException("Usuario no encontrado.");
            clientId = existing.Id;
        }
        else if (existing is not null)
        {
            user = await users.CreateAsync(email, command.Password, firstName, lastName, TenantRole.Patient, false, ct);
            var client = await clients.GetAsync(existing.Id, ct)
                ?? throw new NotFoundException("Paciente no encontrado.");
            client.UserId = user.Id;
            client.FirstName = firstName;
            client.LastName = lastName;
            client.DocumentNumber = document;
            client.Phone = phone;
            await clients.SaveAsync(ct);
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
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                DocumentNumber = document,
                Phone = PhoneRules.Require(command.Phone)
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
