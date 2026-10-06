using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class TenantsService(
    IValidator<AltaTenantCommand> altaValidator,
    ICatalogStore catalogo,
    ITenantProvisioner provisioner,
    ISecretProtector protector,
    TimeProvider clock)
{
    public async Task<TenantDto> CrearAsync(AltaTenantCommand command, CancellationToken ct)
    {
        var cmd = command with { Slug = command.Slug.Trim().ToLowerInvariant(), Nombre = command.Nombre.Trim() };
        var validation = await altaValidator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
        if (await catalogo.ExisteSlugAsync(cmd.Slug, ct))
            throw new ConflictoException("Ya existe un consultorio con ese slug.");

        var connection = await provisioner.ProvisionAsync(cmd.Slug, ct);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = cmd.Slug,
            Nombre = cmd.Nombre,
            Tipo = cmd.Tipo,
            Estado = EstadoTenant.Activo,
            ConnectionStringProtegida = protector.Protect(connection),
            CreadoEn = clock.GetUtcNow()
        };
        await catalogo.AgregarAsync(tenant, ct);
        return new TenantDto(tenant.Id, tenant.Slug, tenant.Nombre, tenant.Tipo, tenant.Estado, tenant.CreadoEn);
    }

    public async Task<IReadOnlyList<TenantDto>> ListarAsync(CancellationToken ct) =>
        (await catalogo.ListarAsync(ct))
            .Select(t => new TenantDto(t.Id, t.Slug, t.Nombre, t.Tipo, t.Estado, t.CreadoEn))
            .ToList();

    public async Task<TenantDto> CambiarEstadoAsync(Guid id, CambiarEstadoTenantCommand command, CancellationToken ct)
    {
        var tenant = await catalogo.ObtenerPorIdAsync(id, ct) ?? throw new NoEncontradoException("Consultorio no encontrado.");
        if (!Enum.IsDefined(command.Estado))
            throw new ReglaNegocioException("Estado inválido.");
        tenant.Estado = command.Estado;
        await catalogo.GuardarAsync(ct);
        return new TenantDto(tenant.Id, tenant.Slug, tenant.Nombre, tenant.Tipo, tenant.Estado, tenant.CreadoEn);
    }
}
