using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application;

public class OrganizationService(IOrganizationStore store, ITenantUserStore users, IAuditStore audit, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<LocationDto>> LocationsAsync(CancellationToken ct) =>
        (await store.LocationsAsync(ct)).Select(l => new LocationDto(l.Id, l.Name, l.Address, l.IsActive)).ToList();

    public async Task<LocationDto> CreateLocationAsync(SaveLocationCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleException("La sede necesita un nombre.");
        var location = await store.AddLocationAsync(new Location
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Address = command.Address.Trim(),
            IsActive = true
        }, ct);
        await RecordAsync("alta", "Sede", location.Id, location.Name, ct);
        return new LocationDto(location.Id, location.Name, location.Address, location.IsActive);
    }

    public async Task<LocationDto> UpdateLocationAsync(Guid id, SaveLocationCommand command, CancellationToken ct)
    {
        var location = await store.GetLocationAsync(id, ct) ?? throw new NotFoundException("Sede no encontrada.");
        location.Name = command.Name.Trim();
        location.Address = command.Address.Trim();
        await store.SaveAsync(ct);
        await RecordAsync("edicion", "Sede", location.Id, location.Name, ct);
        return new LocationDto(location.Id, location.Name, location.Address, location.IsActive);
    }

    public async Task<IReadOnlyList<MedicalServiceDto>> MedicalServicesAsync(CancellationToken ct) =>
        (await store.MedicalServicesAsync(ct)).Select(s => new MedicalServiceDto(s.Id, s.LocationId, s.Name)).ToList();

    public async Task<MedicalServiceDto> CreateMedicalServiceAsync(SaveMedicalServiceCommand command, CancellationToken ct)
    {
        _ = await store.GetLocationAsync(command.LocationId, ct) ?? throw new NotFoundException("Sede no encontrada.");
        var service = await store.AddMedicalServiceAsync(new MedicalService
        {
            Id = Guid.NewGuid(),
            LocationId = command.LocationId,
            Name = command.Name.Trim()
        }, ct);
        await RecordAsync("alta", "Servicio", service.Id, service.Name, ct);
        return new MedicalServiceDto(service.Id, service.LocationId, service.Name);
    }

    public async Task<MedicalServiceDto> UpdateMedicalServiceAsync(Guid id, SaveMedicalServiceCommand command, CancellationToken ct)
    {
        var service = await store.GetMedicalServiceAsync(id, ct) ?? throw new NotFoundException("Servicio no encontrado.");
        service.Name = command.Name.Trim();
        service.LocationId = command.LocationId;
        await store.SaveAsync(ct);
        return new MedicalServiceDto(service.Id, service.LocationId, service.Name);
    }

    public async Task<IReadOnlyList<SpecialtyDto>> SpecialtiesAsync(CancellationToken ct) =>
        (await store.SpecialtiesAsync(ct)).Select(s => new SpecialtyDto(s.Id, s.Name)).ToList();

    public async Task<SpecialtyDto> CreateSpecialtyAsync(SaveSpecialtyCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleException("La especialidad necesita un nombre.");
        var specialty = await store.AddSpecialtyAsync(new Specialty { Id = Guid.NewGuid(), Name = command.Name.Trim() }, ct);
        await RecordAsync("alta", "Especialidad", specialty.Id, specialty.Name, ct);
        return new SpecialtyDto(specialty.Id, specialty.Name);
    }

    public async Task<SpecialtyDto> UpdateSpecialtyAsync(Guid id, SaveSpecialtyCommand command, CancellationToken ct)
    {
        var specialty = await store.GetSpecialtyAsync(id, ct) ?? throw new NotFoundException("Especialidad no encontrada.");
        specialty.Name = command.Name.Trim();
        await store.SaveAsync(ct);
        return new SpecialtyDto(specialty.Id, specialty.Name);
    }

    public async Task<IReadOnlyList<AppointmentTypeDto>> AppointmentTypesAsync(CancellationToken ct) =>
        (await store.AppointmentTypesAsync(ct)).Select(MapType).ToList();

    public async Task<AppointmentTypeDto> CreateAppointmentTypeAsync(SaveAppointmentTypeCommand command, CancellationToken ct)
    {
        ValidateType(command);
        var type = await store.AddAppointmentTypeAsync(new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            DurationMinutes = command.DurationMinutes,
            SpecialtyId = command.SpecialtyId
        }, ct);
        await RecordAsync("alta", "TipoTurno", type.Id, type.Name, ct);
        return MapType(type);
    }

    public async Task<AppointmentTypeDto> UpdateAppointmentTypeAsync(Guid id, SaveAppointmentTypeCommand command, CancellationToken ct)
    {
        ValidateType(command);
        var type = await store.GetAppointmentTypeAsync(id, ct) ?? throw new NotFoundException("Tipo de turno no encontrado.");
        type.Name = command.Name.Trim();
        type.DurationMinutes = command.DurationMinutes;
        type.SpecialtyId = command.SpecialtyId;
        await store.SaveAsync(ct);
        return MapType(type);
    }

    public async Task<IReadOnlyList<ProfessionalDto>> ProfessionalsAsync(Guid? specialtyId, CancellationToken ct)
    {
        var list = await users.ListByRoleAsync(TenantRole.Doctor, specialtyId, ct);
        return list.Select(p => new ProfessionalDto(p.Id, p.FirstName, p.LastName, p.Email, p.LicenseNumber, p.SpecialtyId)).ToList();
    }

    private async Task RecordAsync(string action, string entity, Guid id, string? detail, CancellationToken ct) =>
        await audit.RecordAsync(currentUser.Id, action, entity, id.ToString(), detail, ct);

    private static void ValidateType(SaveAppointmentTypeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleException("El tipo de turno necesita un nombre.");
        if (command.DurationMinutes is < 5 or > 240)
            throw new BusinessRuleException("La duración debe estar entre 5 y 240 minutos.");
    }

    private static AppointmentTypeDto MapType(AppointmentType type) =>
        new(type.Id, type.Name, type.DurationMinutes, type.SpecialtyId);
}
