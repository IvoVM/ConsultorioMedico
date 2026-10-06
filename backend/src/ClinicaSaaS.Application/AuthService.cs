using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class PlatformAuthService(IPlatformUserStore plataforma, ITokenService tokens)
{
    public async Task<TokenDto> LoginAsync(LoginCommand command, CancellationToken ct)
    {
        var user = await plataforma.FindByEmailAsync(command.Email.Trim(), ct)
            ?? throw new ReglaNegocioException("Credenciales inválidas.");
        if (!await plataforma.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new ReglaNegocioException("Credenciales inválidas.");
        return new TokenDto(
            tokens.CreatePlatformToken(user.Id, user.Email, user.Nombre),
            user.Email,
            user.Nombre,
            "SuperAdmin",
            null,
            false);
    }
}

public class AuthService(
    ITenantUserStore usuarios,
    IPacienteStore pacientes,
    ITokenService tokens,
    ITenantActual tenant,
    IValidator<RegistroPacienteCommand> registroValidator,
    IAuditoriaStore auditoria)
{
    public async Task<TokenDto> LoginTenantAsync(LoginCommand command, CancellationToken ct)
    {
        var user = await usuarios.FindByEmailAsync(command.Email.Trim(), ct)
            ?? throw new ReglaNegocioException("Credenciales inválidas.");
        if (!await usuarios.CheckPasswordAsync(user.Id, command.Password, ct))
            throw new ReglaNegocioException("Credenciales inválidas.");
        var nombre = $"{user.Nombre} {user.Apellido}".Trim();
        return new TokenDto(
            tokens.CreateTenantToken(user.Id, user.Email, nombre, tenant.Slug, user.Rol, user.DebeCambiarClave),
            user.Email,
            nombre,
            user.Rol.ToString(),
            tenant.Slug,
            user.DebeCambiarClave);
    }

    public async Task<TokenDto> RegistrarPacienteAsync(RegistroPacienteCommand command, CancellationToken ct)
    {
        var result = await registroValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        if (await usuarios.FindByEmailAsync(command.Email.Trim(), ct) is not null)
            throw new ConflictoException("Ese email ya está registrado.");

        var user = await usuarios.CreateAsync(
            command.Email.Trim(),
            command.Password,
            command.Nombre.Trim(),
            command.Apellido.Trim(),
            RolTenant.Paciente,
            null,
            null,
            false,
            ct);
        await pacientes.AgregarAsync(new Paciente
        {
            Id = Guid.NewGuid(),
            UsuarioId = user.Id,
            Documento = command.Documento.Trim(),
            FechaNacimiento = command.FechaNacimiento,
            Telefono = command.Telefono.Trim()
        }, ct);
        await auditoria.RegistrarAsync(user.Id, "registro", "Paciente", user.Id.ToString(), command.Email, ct);
        var nombre = $"{user.Nombre} {user.Apellido}".Trim();
        return new TokenDto(
            tokens.CreateTenantToken(user.Id, user.Email, nombre, tenant.Slug, user.Rol, false),
            user.Email,
            nombre,
            user.Rol.ToString(),
            tenant.Slug,
            false);
    }
}
