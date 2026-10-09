using ClinicaSaaS.Application.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Application.Services;

public class ScheduleService(
    IScheduleStore schedules,
    IOrganizationStore organization,
    IAppointmentStore appointments,
    IClientStore clients,
    IWaitlistStore waitlist,
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
            .Where(b => b.Professional.Role == TenantRole.Doctor && b.Professional.Employee != null);
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
            VisitReason = command.VisitReason?.Trim()
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
