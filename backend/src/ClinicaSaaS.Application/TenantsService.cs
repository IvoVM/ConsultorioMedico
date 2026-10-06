using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class TenantsService(
    IValidator<CreateTenantCommand> createValidator,
    ICatalogStore catalog,
    ITenantProvisioner provisioner,
    ISecretProtector protector,
    TimeProvider clock)
{
    public async Task<TenantDto> CreateAsync(CreateTenantCommand command, CancellationToken ct)
    {
        var cmd = command with { Slug = command.Slug.Trim().ToLowerInvariant(), Name = command.Name.Trim() };
        var validation = await createValidator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
        if (await catalog.SlugExistsAsync(cmd.Slug, ct))
            throw new ConflictException("Ya existe un consultorio con ese slug.");

        var connection = await provisioner.ProvisionAsync(cmd.Slug, ct);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = cmd.Slug,
            Name = cmd.Name,
            Type = cmd.Type,
            Status = TenantStatus.Active,
            ProtectedConnectionString = protector.Protect(connection),
            CreatedAt = clock.GetUtcNow()
        };
        await catalog.AddAsync(tenant, ct);
        return Map(tenant);
    }

    public async Task<IReadOnlyList<TenantDto>> ListAsync(CancellationToken ct) =>
        (await catalog.ListAsync(ct)).Select(Map).ToList();

    public async Task<TenantDto> ChangeStatusAsync(Guid id, ChangeTenantStatusCommand command, CancellationToken ct)
    {
        var tenant = await catalog.GetByIdAsync(id, ct) ?? throw new NotFoundException("Consultorio no encontrado.");
        if (!Enum.IsDefined(command.Status))
            throw new BusinessRuleException("Estado inválido.");
        tenant.Status = command.Status;
        await catalog.SaveAsync(ct);
        return Map(tenant);
    }

    private static TenantDto Map(Tenant tenant) =>
        new(tenant.Id, tenant.Slug, tenant.Name, tenant.Type, tenant.Status, tenant.CreatedAt);
}
