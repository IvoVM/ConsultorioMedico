using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class OrganizationService(
    IOrganizationStore store,
    ITenantUserStore users,
    IEmployeeStore employees,
    IClientStore clients,
    IAuditStore audit,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<LocationDto>> LocationsAsync(CancellationToken ct) =>
        await store.Locations.OrderBy(l => l.Name).ToLocationDtos().ToListAsync(ct);

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
        await store.MedicalServices.OrderBy(s => s.Name).ToMedicalServiceDtos().ToListAsync(ct);

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
        await store.Specialties.OrderBy(s => s.Name).ToSpecialtyDtos().ToListAsync(ct);

    public async Task<SpecialtyDto> CreateSpecialtyAsync(SaveSpecialtyCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleException("La especialidad necesita un nombre.");
        var specialty = await store.AddSpecialtyAsync(new Specialty { Id = Guid.NewGuid(), Name = command.Name.Trim() }, ct);
        await store.AddAppointmentTypeAsync(new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = specialty.Name,
            DurationMinutes = 30,
            SpecialtyId = specialty.Id
        }, ct);
        await RecordAsync("alta", "Especialidad", specialty.Id, specialty.Name, ct);
        return new SpecialtyDto(specialty.Id, specialty.Name);
    }

    public async Task<SpecialtyDto> UpdateSpecialtyAsync(Guid id, SaveSpecialtyCommand command, CancellationToken ct)
    {
        var specialty = await store.GetSpecialtyAsync(id, ct) ?? throw new NotFoundException("Especialidad no encontrada.");
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleException("La especialidad necesita un nombre.");
        var previous = specialty.Name;
        specialty.Name = command.Name.Trim();
        if (!string.Equals(previous, specialty.Name, StringComparison.Ordinal))
        {
            var match = await store.AppointmentTypes
                .Where(t => t.SpecialtyId == id && t.Name == previous)
                .Select(t => t.Id)
                .ToListAsync(ct);
            if (match.Count == 1)
            {
                var type = await store.GetAppointmentTypeAsync(match[0], ct);
                if (type is not null)
                    type.Name = specialty.Name;
            }
        }
        await store.SaveAsync(ct);
        return new SpecialtyDto(specialty.Id, specialty.Name);
    }

    public async Task<PrestacionesDto> PrestacionesAsync(Guid specialtyId, CancellationToken ct)
    {
        var specialty = await store.GetSpecialtyAsync(specialtyId, ct) ?? throw new NotFoundException("Especialidad no encontrada.");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var rows = await store.AppointmentTypes
            .Where(t => t.SpecialtyId == specialtyId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.DurationMinutes,
                Fees = t.Fees
                    .Where(f => f.EffectiveFrom <= today)
                    .Select(f => new { f.Id, f.Payer, f.Amount, f.EffectiveFrom })
                    .ToList()
            })
            .ToListAsync(ct);
        var items = rows
            .OrderBy(t => t.Name == specialty.Name ? 0 : 1)
            .ThenBy(t => t.Name)
            .Select(t =>
            {
                var current = t.Fees
                    .GroupBy(f => BillingRules.CanonicalPayer(f.Payer)?.ToLowerInvariant() ?? "")
                    .Select(g => g.OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).First())
                    .ToList();
                var particular = current.FirstOrDefault(f => BillingRules.CanonicalPayer(f.Payer) is null);
                var insurers = current
                    .Where(f => BillingRules.CanonicalPayer(f.Payer) is string)
                    .OrderBy(f => f.Payer)
                    .Select(f => new InsurerPriceDto(f.Payer!, f.Amount, f.EffectiveFrom))
                    .ToList();
                return new PrestacionDto(t.Id, t.Name, t.DurationMinutes, particular?.Amount, particular?.EffectiveFrom, insurers);
            })
            .ToList();
        var names = await clients.Clients
            .Where(c => c.Record != null && c.Record.HealthInsurance != null && c.Record.HealthInsurance != "")
            .Select(c => c.Record!.HealthInsurance!)
            .Distinct()
            .ToListAsync(ct);
        var insurersKnown = names
            .Select(BillingRules.CanonicalPayer)
            .Where(name => name is not null)
            .DistinctBy(name => name!.ToLowerInvariant())
            .Cast<string>()
            .OrderBy(name => name)
            .ToList();
        return new PrestacionesDto(items, insurersKnown);
    }

    public async Task<IReadOnlyList<AppointmentTypeDto>> AppointmentTypesAsync(CancellationToken ct) =>
        await store.AppointmentTypes.OrderBy(t => t.Name).ToAppointmentTypeDtos().ToListAsync(ct);

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
        return new AppointmentTypeDto(type.Id, type.Name, type.DurationMinutes, type.SpecialtyId);
    }

    public async Task<AppointmentTypeDto> UpdateAppointmentTypeAsync(Guid id, SaveAppointmentTypeCommand command, CancellationToken ct)
    {
        ValidateType(command);
        var type = await store.GetAppointmentTypeAsync(id, ct) ?? throw new NotFoundException("Tipo de turno no encontrado.");
        type.Name = command.Name.Trim();
        type.DurationMinutes = command.DurationMinutes;
        type.SpecialtyId = command.SpecialtyId;
        await store.SaveAsync(ct);
        return new AppointmentTypeDto(type.Id, type.Name, type.DurationMinutes, type.SpecialtyId);
    }

    public async Task<IReadOnlyList<ProfessionalDto>> ProfessionalsAsync(Guid? specialtyId, CancellationToken ct)
    {
        var query = employees.Employees.Where(e => e.User.Role == TenantRole.Doctor && e.IsActive);
        if (specialtyId is Guid id)
            query = query.Where(e => e.SpecialtyId == id);
        return await query.OrderBy(e => e.User.LastName).ThenBy(e => e.User.FirstName).ToProfessionalDtos().ToListAsync(ct);
    }

    public async Task<ProfessionalDto> AssignSpecialtyAsync(Guid id, AssignSpecialtyCommand command, CancellationToken ct)
    {
        var account = await users.Users.Where(u => u.Id == id).Select(u => new { u.Role, u.Email }).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Profesional no encontrado.");
        if (account.Role != TenantRole.Doctor)
            throw new BusinessRuleException("Solo un médico puede sumarse a una especialidad.");
        if (!await employees.Employees.AnyAsync(e => e.UserId == id, ct))
            throw new NotFoundException("Empleado no encontrado.");
        if (command.SpecialtyId is Guid specialtyId)
            _ = await store.GetSpecialtyAsync(specialtyId, ct) ?? throw new NotFoundException("Especialidad no encontrada.");
        await employees.AssignSpecialtyAsync(id, command.SpecialtyId, ct);
        await RecordAsync("edicion", "Especialidad", command.SpecialtyId ?? id, account.Email, ct);
        return await employees.Employees.Where(e => e.UserId == id).ToProfessionalDtos().FirstAsync(ct);
    }

    public async Task<object> ListMedicalServicesAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await MedicalServicesAsync(ct);
        var source = store.MedicalServices.AsQueryable();
        if (query.Term is string term)
            source = source.Where(s => s.Name.ToLower().Contains(term));
        source = query.Desc ? source.OrderByDescending(s => s.Name) : source.OrderBy(s => s.Name);
        return await Pages.Materialize(source.ToMedicalServiceDtos(), query, ct);
    }

    public async Task<object> ListSpecialtiesAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await SpecialtiesAsync(ct);
        var source = store.Specialties.AsQueryable();
        if (query.Term is string term)
            source = source.Where(s => s.Name.ToLower().Contains(term));
        source = (query.Sort, query.Desc) switch
        {
            ("people", false) => source.OrderBy(s => s.Employees.Count(e => e.User.Role == TenantRole.Doctor && e.IsActive)).ThenBy(s => s.Name),
            ("people", true) => source.OrderByDescending(s => s.Employees.Count(e => e.User.Role == TenantRole.Doctor && e.IsActive)).ThenBy(s => s.Name),
            ("name", true) => source.OrderByDescending(s => s.Name),
            _ => source.OrderBy(s => s.Name)
        };
        var rows = source.Select(s => new SpecialtyRowDto(
            s.Id,
            s.Name,
            s.Employees.Count(e => e.User.Role == TenantRole.Doctor && e.IsActive)));
        return await Pages.Materialize(rows, query, ct);
    }

    public async Task<object> ListAppointmentTypesAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await AppointmentTypesAsync(ct);
        var source = store.AppointmentTypes.AsQueryable();
        if (query.Term is string term)
            source = source.Where(t =>
                t.Name.ToLower().Contains(term) ||
                (t.Specialty != null && t.Specialty.Name.ToLower().Contains(term)) ||
                t.DurationMinutes.ToString().Contains(term));
        source = (query.Sort, query.Desc) switch
        {
            ("duration", false) => source.OrderBy(t => t.DurationMinutes).ThenBy(t => t.Name),
            ("duration", true) => source.OrderByDescending(t => t.DurationMinutes).ThenBy(t => t.Name),
            ("specialty", false) => source.OrderBy(t => t.Specialty != null ? t.Specialty.Name : "").ThenBy(t => t.Name),
            ("specialty", true) => source.OrderByDescending(t => t.Specialty != null ? t.Specialty.Name : "").ThenBy(t => t.Name),
            ("people", false) => source.OrderBy(t => t.ScheduleBlocks.Select(b => b.ProfessionalId).Distinct().Count()).ThenBy(t => t.Name),
            ("people", true) => source.OrderByDescending(t => t.ScheduleBlocks.Select(b => b.ProfessionalId).Distinct().Count()).ThenBy(t => t.Name),
            ("name", true) => source.OrderByDescending(t => t.Name),
            _ => source.OrderBy(t => t.Name)
        };
        var rows = source.Select(t => new AppointmentTypeRowDto(
            t.Id,
            t.Name,
            t.DurationMinutes,
            t.Specialty != null ? t.Specialty.Name : "",
            t.ScheduleBlocks.Select(b => b.ProfessionalId).Distinct().Count()));
        return await Pages.Materialize(rows, query, ct);
    }

    public async Task<IReadOnlyList<InsurerDto>> EnabledInsurersAsync(CancellationToken ct) =>
        await store.Insurers.Where(i => i.Enabled).OrderBy(i => i.Name).Select(i => new InsurerDto(i.Id, i.Name, i.Enabled)).ToListAsync(ct);

    public async Task<object> ListInsurersAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await store.Insurers.OrderBy(i => i.Name).Select(i => new InsurerDto(i.Id, i.Name, i.Enabled)).ToListAsync(ct);
        var source = store.Insurers.AsQueryable();
        if (query.Term is string term)
            source = source.Where(i => i.Name.ToLower().Contains(term) || (i.Enabled ? "permitida" : "no permitida").Contains(term));
        source = (query.Sort, query.Desc) switch
        {
            ("status", false) => source.OrderBy(i => i.Enabled).ThenBy(i => i.Name),
            ("status", true) => source.OrderByDescending(i => i.Enabled).ThenBy(i => i.Name),
            ("name", true) => source.OrderByDescending(i => i.Name),
            _ => source.OrderBy(i => i.Name)
        };
        return await Pages.Materialize(source.Select(i => new InsurerDto(i.Id, i.Name, i.Enabled)), query, ct);
    }

    public async Task<InsurerDto> CreateInsurerAsync(SaveInsurerCommand command, CancellationToken ct)
    {
        var name = command.Name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > 120)
            throw new BusinessRuleException("La obra social necesita un nombre de hasta 120 caracteres.");
        if (await store.Insurers.AnyAsync(i => i.Name.ToLower() == name.ToLower(), ct))
            throw new BusinessRuleException("Esa obra social ya está cargada.");
        var insurer = await store.AddInsurerAsync(new Insurer { Id = Guid.NewGuid(), Name = name, Enabled = true }, ct);
        await RecordAsync("alta", "ObraSocial", insurer.Id, insurer.Name, ct);
        return new InsurerDto(insurer.Id, insurer.Name, insurer.Enabled);
    }

    public async Task<InsurerDto> SetInsurerEnabledAsync(Guid id, bool enabled, CancellationToken ct)
    {
        var insurer = await store.GetInsurerAsync(id, ct) ?? throw new NotFoundException("Obra social no encontrada.");
        insurer.Enabled = enabled;
        await store.SaveAsync(ct);
        await RecordAsync(enabled ? "alta" : "baja", "ObraSocial", insurer.Id, insurer.Name, ct);
        return new InsurerDto(insurer.Id, insurer.Name, insurer.Enabled);
    }

    public async Task<IReadOnlyList<InsurerDto>> ProfessionalInsurersAsync(Guid professionalId, CancellationToken ct) =>
        await employees.Employees
            .Where(e => e.UserId == professionalId && e.IsActive && e.User.Role == TenantRole.Doctor)
            .SelectMany(e => e.Insurers)
            .Where(i => i.Enabled)
            .OrderBy(i => i.Name)
            .Select(i => new InsurerDto(i.Id, i.Name, i.Enabled))
            .ToListAsync(ct);

    private async Task RecordAsync(string action, string entity, Guid id, string? detail, CancellationToken ct) =>
        await audit.RecordAsync(currentUser.Id, action, entity, id.ToString(), detail, ct);

    private static void ValidateType(SaveAppointmentTypeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleException("La prestación necesita un nombre.");
        if (command.DurationMinutes is < 5 or > 240)
            throw new BusinessRuleException("La duración debe estar entre 5 y 240 minutos.");
    }
}
