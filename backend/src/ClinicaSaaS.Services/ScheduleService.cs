using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class ScheduleService(
    IScheduleStore schedules,
    IOrganizationStore organization,
    IAppointmentStore appointments,
    IEmployeeStore employees,
    IClientStore clients,
    IWaitlistStore waitlist,
    IMedicalRecordStore records,
    ITimeZoneProvider timeZone,
    ICurrentUser currentUser,
    IAuditStore audit,
    IValidator<BookAppointmentCommand> bookingValidator,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<ScheduleBlockDto>> GetScheduleAsync(Guid professionalId, CancellationToken ct) =>
        await schedules.Blocks
            .Where(b => b.ProfessionalId == professionalId)
            .OrderBy(b => b.Day)
            .ThenBy(b => b.StartTime)
            .ToScheduleBlockDtos()
            .ToListAsync(ct);

    public async Task<object> ListScheduleAsync(Guid professionalId, ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await GetScheduleAsync(professionalId, ct);
        var source = schedules.Blocks.Where(b => b.ProfessionalId == professionalId);
        if (query.Term is string term)
            source = source.Where(b =>
                (b.Day == DayOfWeek.Monday ? "lunes" : b.Day == DayOfWeek.Tuesday ? "martes" : b.Day == DayOfWeek.Wednesday ? "miércoles" : b.Day == DayOfWeek.Thursday ? "jueves" : b.Day == DayOfWeek.Friday ? "viernes" : b.Day == DayOfWeek.Saturday ? "sábado" : "domingo").Contains(term) ||
                b.StartTime.ToString().Contains(term) ||
                b.EndTime.ToString().Contains(term));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("time", false) => source.OrderBy(b => b.StartTime).ThenBy(b => b.Day == DayOfWeek.Monday ? 1 : b.Day == DayOfWeek.Tuesday ? 2 : b.Day == DayOfWeek.Wednesday ? 3 : b.Day == DayOfWeek.Thursday ? 4 : b.Day == DayOfWeek.Friday ? 5 : b.Day == DayOfWeek.Saturday ? 6 : 7),
            ("time", true) => source.OrderByDescending(b => b.StartTime).ThenBy(b => b.Day == DayOfWeek.Monday ? 1 : b.Day == DayOfWeek.Tuesday ? 2 : b.Day == DayOfWeek.Wednesday ? 3 : b.Day == DayOfWeek.Thursday ? 4 : b.Day == DayOfWeek.Friday ? 5 : b.Day == DayOfWeek.Saturday ? 6 : 7),
            ("day", true) => source.OrderByDescending(b => b.Day == DayOfWeek.Monday ? 1 : b.Day == DayOfWeek.Tuesday ? 2 : b.Day == DayOfWeek.Wednesday ? 3 : b.Day == DayOfWeek.Thursday ? 4 : b.Day == DayOfWeek.Friday ? 5 : b.Day == DayOfWeek.Saturday ? 6 : 7).ThenByDescending(b => b.StartTime),
            _ => source.OrderBy(b => b.Day == DayOfWeek.Monday ? 1 : b.Day == DayOfWeek.Tuesday ? 2 : b.Day == DayOfWeek.Wednesday ? 3 : b.Day == DayOfWeek.Thursday ? 4 : b.Day == DayOfWeek.Friday ? 5 : b.Day == DayOfWeek.Saturday ? 6 : 7).ThenBy(b => b.StartTime)
        };
        return await Pages.Materialize(ordered.ToScheduleBlockDtos(), query, ct);
    }

    public async Task<IReadOnlyList<ScheduleBlockDto>> SaveScheduleAsync(SaveScheduleCommand command, CancellationToken ct)
    {
        foreach (var block in command.Blocks)
        {
            if (block.EndTime <= block.StartTime)
                throw new BusinessRuleException("El horario de fin debe ser posterior al de inicio.");
            _ = await organization.GetAppointmentTypeAsync(block.AppointmentTypeId, ct)
                ?? throw new NotFoundException("Tipo de turno no encontrado.");
        }

        var entities = command.Blocks.Select(b => new ScheduleBlock
        {
            Id = Guid.NewGuid(),
            ProfessionalId = command.ProfessionalId,
            Day = b.Day,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            LocationId = b.LocationId,
            AppointmentTypeId = b.AppointmentTypeId
        }).ToList();
        await schedules.ReplaceBlocksAsync(command.ProfessionalId, entities, ct);
        await audit.RecordAsync(currentUser.Id, "agenda", "AgendaSemanal", command.ProfessionalId.ToString(), $"{entities.Count} bloques", ct);
        return entities.Select(b => new ScheduleBlockDto(b.Id, b.Day, b.StartTime, b.EndTime, b.LocationId, b.AppointmentTypeId)).ToList();
    }

    public async Task<IReadOnlyList<BlockoutDto>> BlockoutsAsync(Guid professionalId, CancellationToken ct) =>
        await schedules.Blockouts
            .Where(b => b.ProfessionalId == professionalId)
            .OrderBy(b => b.Start)
            .ToBlockoutDtos()
            .ToListAsync(ct);

    public async Task<object> ListBlockoutsAsync(Guid professionalId, ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await BlockoutsAsync(professionalId, ct);
        var source = schedules.Blockouts.Where(b => b.ProfessionalId == professionalId);
        if (query.Term is string term)
            source = source.Where(b => b.Reason.ToLower().Contains(term));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("end", false) => source.OrderBy(b => b.End).ThenBy(b => b.Start),
            ("end", true) => source.OrderByDescending(b => b.End).ThenBy(b => b.Start),
            ("reason", false) => source.OrderBy(b => b.Reason).ThenBy(b => b.Start),
            ("reason", true) => source.OrderByDescending(b => b.Reason).ThenBy(b => b.Start),
            ("start", true) => source.OrderByDescending(b => b.Start),
            _ => source.OrderBy(b => b.Start)
        };
        return await Pages.Materialize(ordered.ToBlockoutDtos(), query, ct);
    }

    public async Task<BlockoutDto> CreateBlockoutAsync(CreateBlockoutCommand command, CancellationToken ct)
    {
        if (command.End <= command.Start)
            throw new BusinessRuleException("El bloqueo debe tener un rango válido.");
        var blockout = await schedules.AddBlockoutAsync(new ScheduleBlockout
        {
            Id = Guid.NewGuid(),
            ProfessionalId = command.ProfessionalId,
            Start = command.Start,
            End = command.End,
            Reason = command.Reason.Trim()
        }, ct);
        return new BlockoutDto(blockout.Id, blockout.ProfessionalId, blockout.Start, blockout.End, blockout.Reason);
    }

    public Task DeleteBlockoutAsync(Guid id, CancellationToken ct) => schedules.DeleteBlockoutAsync(id, ct);

    public async Task<IReadOnlyList<SlotDto>> AvailabilityAsync(AvailabilityQuery query, CancellationToken ct)
    {
        var openingsQuery = schedules.Blocks
            .Where(b => b.Day == query.Date.DayOfWeek && b.LocationId == query.LocationId)
            .Where(b => b.Professional.Role == TenantRole.Doctor && b.Professional.Employee != null && b.Professional.Employee.IsActive);
        if (query.SpecialtyId != Guid.Empty)
            openingsQuery = openingsQuery.Where(b => b.Professional.Employee!.SpecialtyId == query.SpecialtyId);
        if (query.ProfessionalId is Guid professionalId)
            openingsQuery = openingsQuery.Where(b => b.ProfessionalId == professionalId);

        var openings = await openingsQuery.ToOpenings().ToListAsync(ct);
        if (openings.Count == 0)
            return [];

        var ids = openings.Select(o => o.ProfessionalId).Distinct().ToList();
        var dayStart = SchedulingRules.Combine(query.Date, TimeOnly.MinValue, timeZone.Zone);
        var dayEnd = dayStart.AddDays(1);
        var busy = await appointments.Appointments
            .Where(a => ids.Contains(a.ProfessionalId) && a.Start >= dayStart && a.Start < dayEnd)
            .ToBusyIntervals()
            .ToListAsync(ct);
        var offs = await schedules.Blockouts
            .Where(b => ids.Contains(b.ProfessionalId) && b.Start < dayEnd && b.End > dayStart)
            .Select(b => new BusyInterval { ProfessionalId = b.ProfessionalId, Start = b.Start, End = b.End })
            .ToListAsync(ct);

        var result = new List<SlotDto>();
        foreach (var group in openings.GroupBy(o => o.ProfessionalId))
        {
            var occupied = busy
                .Where(b => b.ProfessionalId == group.Key && b.Status is AppointmentStatus status && SchedulingRules.CountsAsBusy(status))
                .Select(b => new TimeRange(b.Start, b.End))
                .Concat(offs.Where(b => b.ProfessionalId == group.Key).Select(b => new TimeRange(b.Start, b.End)))
                .ToList();
            foreach (var opening in group)
            {
                var slots = SchedulingRules.GenerateSlots(
                    query.Date, opening.StartTime, opening.EndTime, opening.DurationMinutes, timeZone.Zone, occupied);
                var name = $"{opening.ProfessionalFirstName} {opening.ProfessionalLastName}".Trim();
                foreach (var slot in slots)
                {
                    result.Add(new SlotDto(
                        opening.ProfessionalId,
                        name,
                        opening.LocationId,
                        opening.AppointmentTypeId,
                        opening.AppointmentType,
                        slot.Start,
                        slot.End));
                    occupied.Add(slot);
                }
            }
        }

        return result.OrderBy(s => s.Start).ThenBy(s => s.Professional).ToList();
    }

    public async Task<AppointmentDto> BookAsync(BookAppointmentCommand command, CancellationToken ct)
    {
        var result = await bookingValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var client = await ResolveClientAsync(command.PatientId, ct);
        var slots = await AvailabilityAsync(new AvailabilityQuery(command.LocationId, Guid.Empty, command.ProfessionalId, DateOnly.FromDateTime(command.Start.DateTime)), ct);
        var slot = slots.FirstOrDefault(s => s.ProfessionalId == command.ProfessionalId && s.AppointmentTypeId == command.AppointmentTypeId && s.Start.UtcDateTime == command.Start.UtcDateTime)
            ?? throw new ConflictException("Ese horario ya no está disponible.");

        Guid? insurerId = null;
        string? memberNumber = null;
        if (command.InsurerId is Guid chosenInsurer)
        {
            memberNumber = command.MemberNumber?.Trim() ?? "";
            if (memberNumber.Length == 0 || memberNumber.Length > 40)
                throw new BusinessRuleException("Si usás obra social, el número de afiliado es obligatorio.");
            var allowed = await employees.Employees
                .Where(e => e.UserId == command.ProfessionalId && e.IsActive && e.User.Role == TenantRole.Doctor)
                .SelectMany(e => e.Insurers)
                .AnyAsync(i => i.Id == chosenInsurer && i.Enabled, ct);
            if (!allowed)
                throw new BusinessRuleException("Ese profesional no atiende por esa obra social.");
            insurerId = chosenInsurer;
        }
        else if (!string.IsNullOrWhiteSpace(command.MemberNumber))
        {
            throw new BusinessRuleException("El número de afiliado es para cuando elegís obra social.");
        }

        var appointment = await appointments.BookAsync(new Appointment
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            ProfessionalId = command.ProfessionalId,
            LocationId = command.LocationId,
            AppointmentTypeId = command.AppointmentTypeId,
            Start = slot.Start,
            End = slot.End,
            Status = AppointmentStatus.Booked,
            VisitReason = command.VisitReason?.Trim(),
            InsurerId = insurerId,
            MemberNumber = memberNumber
        }, ct);
        await audit.RecordAsync(currentUser.Id, "reserva", "Turno", appointment.Id.ToString(), appointment.Start.ToString("O"), ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    public async Task<IReadOnlyList<AppointmentDto>> ForDayAsync(DateOnly date, Guid? professionalId, CancellationToken ct)
    {
        if (currentUser.Role == TenantRole.Doctor.ToString())
            professionalId = currentUser.Id;
        var start = SchedulingRules.Combine(date, TimeOnly.MinValue, timeZone.Zone);
        var end = start.AddDays(1);
        var query = appointments.Appointments.Where(a => a.Start >= start && a.Start < end);
        if (professionalId is Guid id)
            query = query.Where(a => a.ProfessionalId == id);
        return await query.OrderBy(a => a.Start).ToAppointmentDtos().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AppointmentDto>> MineAsync(CancellationToken ct)
    {
        var clientId = await clients.Clients
            .Where(c => c.UserId == (currentUser.Id ?? Guid.Empty))
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
        return await appointments.Appointments
            .Where(a => a.ClientId == clientId)
            .OrderByDescending(a => a.Start)
            .ToAppointmentDtos()
            .ToListAsync(ct);
    }

    public async Task<AppointmentDto> CancelAsync(Guid id, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (currentUser.Role == TenantRole.Patient.ToString())
        {
            var client = await clients.GetByUserAsync(currentUser.Id ?? Guid.Empty, ct)
                ?? throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
            if (appointment.ClientId != client.Id)
                throw new BusinessRuleException("No podés cancelar un turno de otra persona.");
            if (!SchedulingRules.PatientCanCancel(appointment.Status, appointment.Start, clock.GetUtcNow()))
                throw new BusinessRuleException("El paciente solo puede cancelar un turno reservado con al menos 24 horas de anticipación.");
        }
        else if (appointment.Status is not (AppointmentStatus.Booked or AppointmentStatus.CheckedIn))
        {
            throw new BusinessRuleException("Ese turno ya no se puede cancelar.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "cancelacion", "Turno", appointment.Id.ToString(), null, ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    public async Task<AppointmentDto> CheckInAsync(Guid id, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.Status != AppointmentStatus.Booked)
            throw new BusinessRuleException("Solo se admite un turno reservado.");
        appointment.Status = AppointmentStatus.CheckedIn;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "admision", "Turno", appointment.Id.ToString(), null, ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    public async Task<AppointmentDto> MarkNoShowAsync(Guid id, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.Status is not (AppointmentStatus.Booked or AppointmentStatus.CheckedIn))
            throw new BusinessRuleException("Solo se marca ausente un turno que todavía no se atendió.");
        appointment.Status = AppointmentStatus.NoShow;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "ausente", "Turno", appointment.Id.ToString(), null, ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    public async Task<AppointmentDto> ReviewCoverageAsync(Guid id, bool accepted, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.InsurerId is null)
            throw new BusinessRuleException("Este turno es particular. No hay un número de afiliado para revisar.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
            throw new BusinessRuleException("Ese turno ya no se revisa.");
        appointment.CoverageAccepted = accepted;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, accepted ? "alta" : "baja", "Afiliado", appointment.Id.ToString(), appointment.MemberNumber, ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    public async Task<AppointmentDto> RescheduleAsync(Guid id, RescheduleAppointmentCommand command, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (!SchedulingRules.CanReschedule(appointment.Status))
            throw new BusinessRuleException("Ese turno no se puede reprogramar.");
        var date = DateOnly.FromDateTime(command.Start.DateTime);
        var slots = await AvailabilityAsync(new AvailabilityQuery(appointment.LocationId, Guid.Empty, appointment.ProfessionalId, date), ct);
        var slot = slots.FirstOrDefault(s => s.Start.UtcDateTime == command.Start.UtcDateTime && s.AppointmentTypeId == appointment.AppointmentTypeId)
            ?? throw new ConflictException("Ese horario ya no está disponible.");
        appointment.Start = slot.Start;
        appointment.End = slot.End;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "reprogramacion", "Turno", appointment.Id.ToString(), appointment.Start.ToString("O"), ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    public async Task<WaitlistEntryDto> JoinWaitlistAsync(CreateWaitlistEntryCommand command, CancellationToken ct)
    {
        var client = await ResolveClientAsync(command.PatientId, ct);
        var entry = await waitlist.AddAsync(new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            ProfessionalId = command.ProfessionalId,
            LocationId = command.LocationId,
            SpecialtyId = command.SpecialtyId,
            Status = WaitlistStatus.Pending,
            CreatedAt = clock.GetUtcNow(),
            Notes = command.Notes?.Trim()
        }, ct);
        return await waitlist.Entries.Where(w => w.Id == entry.Id).ToWaitlistDtos().FirstAsync(ct);
    }

    public async Task<IReadOnlyList<WaitlistEntryDto>> WaitlistAsync(CancellationToken ct) =>
        await waitlist.Entries
            .Where(w => w.Status == WaitlistStatus.Pending || w.Status == WaitlistStatus.Offered)
            .OrderBy(w => w.CreatedAt)
            .ToWaitlistDtos()
            .ToListAsync(ct);

    public async Task<AppointmentDto> AssignWaitlistEntryAsync(Guid id, AssignWaitlistEntryCommand command, CancellationToken ct)
    {
        var entry = await waitlist.GetAsync(id, ct) ?? throw new NotFoundException("La entrada de lista de espera no existe.");
        if (entry.Status is WaitlistStatus.Accepted or WaitlistStatus.Cancelled)
            throw new BusinessRuleException("Esa espera ya fue resuelta.");
        if (entry.ProfessionalId is null)
            throw new BusinessRuleException("Elegí un profesional antes de asignar el turno.");

        var appointment = await BookAsync(new BookAppointmentCommand(
            entry.ClientId,
            entry.ProfessionalId.Value,
            entry.LocationId,
            command.AppointmentTypeId,
            command.Start,
            entry.Notes), ct);
        entry.Status = WaitlistStatus.Accepted;
        await waitlist.SaveAsync(ct);
        return appointment;
    }

    public async Task<AppointmentDto> OfferNextAsync(Guid id, CancellationToken ct)
    {
        var entry = await waitlist.GetAsync(id, ct) ?? throw new NotFoundException("La entrada de lista de espera no existe.");
        if (entry.Status is WaitlistStatus.Accepted or WaitlistStatus.Cancelled)
            throw new BusinessRuleException("Esa espera ya fue resuelta.");

        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone.Zone);
        var date = DateOnly.FromDateTime(localNow.DateTime);
        SlotDto? chosen = null;
        for (var day = 0; day < 21 && chosen is null; day++)
        {
            var slots = await AvailabilityAsync(new AvailabilityQuery(entry.LocationId, entry.SpecialtyId, entry.ProfessionalId, date.AddDays(day)), ct);
            chosen = slots.FirstOrDefault(slot => slot.Start > clock.GetUtcNow());
        }

        if (chosen is null)
            throw new BusinessRuleException("No hay un hueco libre en las próximas tres semanas.");

        var (insurerId, memberNumber) = await CoverageFromChartAsync(entry.ClientId, chosen.ProfessionalId, ct);
        var appointment = await BookAsync(new BookAppointmentCommand(
            entry.ClientId,
            chosen.ProfessionalId,
            chosen.LocationId,
            chosen.AppointmentTypeId,
            chosen.Start,
            entry.Notes,
            insurerId,
            memberNumber), ct);
        entry.Status = WaitlistStatus.Accepted;
        await waitlist.SaveAsync(ct);
        return appointment;
    }

    private async Task<(Guid? InsurerId, string? MemberNumber)> CoverageFromChartAsync(Guid clientId, Guid professionalId, CancellationToken ct)
    {
        var record = await records.GetByClientAsync(clientId, ct);
        if (record is null || string.IsNullOrWhiteSpace(record.HealthInsurance) || string.IsNullOrWhiteSpace(record.MemberNumber))
            return (null, null);
        var name = record.HealthInsurance.Trim().ToLower();
        var insurerId = await employees.Employees
            .Where(employee => employee.UserId == professionalId && employee.IsActive && employee.User.Role == TenantRole.Doctor)
            .SelectMany(employee => employee.Insurers)
            .Where(insurer => insurer.Enabled && insurer.Name.ToLower() == name)
            .Select(insurer => (Guid?)insurer.Id)
            .FirstOrDefaultAsync(ct);
        return insurerId is null ? (null, null) : (insurerId, record.MemberNumber.Trim());
    }

    private async Task<Client> ResolveClientAsync(Guid? patientId, CancellationToken ct)
    {
        if (currentUser.Role == TenantRole.Patient.ToString())
        {
            return await clients.GetByUserAsync(currentUser.Id ?? Guid.Empty, ct)
                ?? throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
        }

        if (patientId is null || patientId == Guid.Empty)
            throw new BusinessRuleException("Indicá el paciente.");
        return await clients.GetAsync(patientId.Value, ct) ?? throw new NotFoundException("Paciente no encontrado.");
    }

    private Task<AppointmentDto> LoadAppointmentAsync(Guid id, CancellationToken ct) =>
        appointments.Appointments.Where(a => a.Id == id).ToAppointmentDtos().FirstAsync(ct);
}
