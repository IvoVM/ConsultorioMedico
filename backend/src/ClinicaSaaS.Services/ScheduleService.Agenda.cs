using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public partial class ScheduleService
{
    private readonly record struct Placement(DateTimeOffset End, Guid LocationId, Guid? RoomId, Guid? EquipmentId, bool Overbook);

    public async Task<AgendaDayDto> AgendaAsync(DateOnly date, CancellationToken ct)
    {
        var doctor = currentUser.Role == TenantRole.Doctor.ToString();
        var doctors = employees.Employees
            .Where(e => e.IsActive && e.User.Role == TenantRole.Doctor);
        if (doctor)
            doctors = doctors.Where(e => e.UserId == currentUser.Id);
        var people = await doctors
            .OrderBy(e => e.User.LastName).ThenBy(e => e.User.FirstName)
            .Select(e => new
            {
                e.UserId,
                Name = (e.User.FirstName + " " + e.User.LastName).Trim(),
                Specialty = e.Specialty != null ? e.Specialty.Name : null,
                e.SpecialtyId,
                Insurers = e.Insurers.Where(i => i.Enabled).OrderBy(i => i.Name).Select(i => new InsurerDto(i.Id, i.Name, i.Enabled)).ToList(),
            })
            .ToListAsync(ct);
        var ids = people.Select(p => p.UserId).ToList();
        var specialtyIds = people.Where(p => p.SpecialtyId != null).Select(p => p.SpecialtyId!.Value).Distinct().ToList();

        var blocks = await schedules.Blocks
            .Where(b => ids.Contains(b.ProfessionalId) && b.Day == date.DayOfWeek)
            .Select(b => new { b.ProfessionalId, b.StartTime, b.EndTime })
            .ToListAsync(ct);
        var dayStart = SchedulingRules.Combine(date, TimeOnly.MinValue, timeZone.Zone);
        var dayEnd = dayStart.AddDays(1);
        var offs = await schedules.Blockouts
            .Where(b => ids.Contains(b.ProfessionalId) && b.Start < dayEnd && b.End > dayStart)
            .Select(b => new { b.ProfessionalId, b.Start, b.End })
            .ToListAsync(ct);
        var types = await organization.AppointmentTypes
            .Where(t => t.SpecialtyId != null && specialtyIds.Contains(t.SpecialtyId.Value))
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.SpecialtyId,
                t.Id,
                t.Name,
                t.DurationMinutes,
                t.Code,
                Equipment = t.Equipment != null ? t.Equipment.Name : null,
                Supplies = t.Supplies.OrderBy(s => s.Supply.Name).Select(s => new PracticeSupplyDto(s.SupplyId, s.Supply.Name, s.Supply.Unit, s.Quantity)).ToList()
            })
            .ToListAsync(ct);
        var shown = await appointments.Appointments
            .Where(a => ids.Contains(a.ProfessionalId) && a.Start >= dayStart && a.Start < dayEnd && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.Start)
            .ToAppointmentDtos(currentUser.Id)
            .ToListAsync(ct);
        var rooms = await organization.Rooms
            .Where(r => r.IsActive && r.Location.IsActive)
            .OrderBy(r => r.Location.Name).ThenBy(r => r.Name)
            .Select(r => new AgendaResourceDto(r.Id, r.Name, r.LocationId, r.Location.Name))
            .ToListAsync(ct);
        var equipment = await organization.Equipment
            .Where(e => e.IsActive && e.Location.IsActive)
            .OrderBy(e => e.Location.Name).ThenBy(e => e.Name)
            .Select(e => new AgendaResourceDto(e.Id, e.Name, e.LocationId, e.Location.Name))
            .ToListAsync(ct);
        var settings = await clinicSettings.GetAsync(ct);

        var professionals = people.Select(p => new AgendaProfessionalDto(
            p.UserId,
            p.Name,
            p.Specialty,
            SchedulingRules.MergeRanges(blocks
                    .Where(b => b.ProfessionalId == p.UserId)
                    .Select(b => new TimeRange(SchedulingRules.Combine(date, b.StartTime, timeZone.Zone), SchedulingRules.Combine(date, b.EndTime, timeZone.Zone))))
                .Select(r => new AgendaRangeDto(r.Start, r.End))
                .ToList(),
            offs.Where(o => o.ProfessionalId == p.UserId).Select(o => new AgendaRangeDto(o.Start, o.End)).ToList(),
            types.Where(t => t.SpecialtyId == p.SpecialtyId).Select(t => new AgendaTypeDto(t.Id, t.Name, t.DurationMinutes, t.Code, t.Equipment, t.Supplies)).ToList(),
            p.Insurers)).ToList();

        return new AgendaDayDto(date, !doctor, professionals, shown, rooms, equipment, settings.MaxOverbooksPerDay);
    }

    public async Task<AppointmentDto> MoveOnAgendaAsync(Guid id, MoveAppointmentCommand command, CancellationToken ct)
    {
        return await unitOfWork.InTransactionAsync(async () =>
        {
            var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
            if (!SchedulingRules.CanReschedule(appointment.Status))
                throw new BusinessRuleException("Ese turno ya no se puede mover.");
            // Moving within the same professional's day keeps a room front desk chose by hand; anything else takes the block's room.
            var sameDay = SchedulingRules.LocalDate(appointment.Start, timeZone.Zone) == SchedulingRules.LocalDate(command.Start, timeZone.Zone);
            var room = command.RoomId ?? (sameDay && command.ProfessionalId == appointment.ProfessionalId ? appointment.RoomId : null);
            var placement = await PlaceAsync(command.ProfessionalId, appointment.AppointmentTypeId, command.Start, appointment.Id, null, ct, room);
            appointment.ProfessionalId = command.ProfessionalId;
            appointment.LocationId = placement.LocationId;
            appointment.RoomId = placement.RoomId;
            appointment.EquipmentId = placement.EquipmentId;
            appointment.Start = command.Start;
            appointment.End = placement.End;
            appointment.IsOverbook = placement.Overbook;
            if (appointment.InsurerId is Guid insurerId && !await AcceptsInsurerAsync(command.ProfessionalId, insurerId, ct))
            {
                appointment.InsurerId = null;
                appointment.MemberNumber = null;
                appointment.CoverageAccepted = null;
            }
            appointment.Status = AppointmentStatus.Rescheduled;
            await appointments.SaveAsync(ct);
            await audit.RecordAsync(currentUser.Id, "reprogramacion", "Turno", appointment.Id.ToString(), appointment.Start.ToString("O"), ct);
            return await LoadAppointmentAsync(appointment.Id, ct);
        }, ct);
    }

    public async Task<AppointmentDto> BookOnAgendaAsync(AgendaBookCommand command, CancellationToken ct)
    {
        var placement = await PlaceAsync(command.ProfessionalId, command.AppointmentTypeId, command.Start, null, null, ct);
        var (insurerId, memberNumber) = await CoverageAsync(command.ProfessionalId, command.InsurerId, command.MemberNumber, ct);
        return await unitOfWork.InTransactionAsync(async () =>
        {
            var clientId = await ClientForDocumentAsync(command.DocumentNumber, command.FirstName, command.LastName, command.Phone, command.PatientInformed, ct);
            var appointment = await appointments.BookAsync(new Appointment
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ProfessionalId = command.ProfessionalId,
                LocationId = placement.LocationId,
                RoomId = placement.RoomId,
                EquipmentId = placement.EquipmentId,
                AppointmentTypeId = command.AppointmentTypeId,
                Start = command.Start,
                End = placement.End,
                Status = AppointmentStatus.Booked,
                InsurerId = insurerId,
                MemberNumber = memberNumber,
                IsOverbook = placement.Overbook
            }, ct);
            await audit.RecordAsync(currentUser.Id, placement.Overbook ? "sobreturno" : "reserva", "Turno", appointment.Id.ToString(), appointment.Start.ToString("O"), ct);
            return await LoadAppointmentAsync(appointment.Id, ct);
        }, ct);
    }

    public async Task<AppointmentDto> PlaceRequestOnAgendaAsync(Guid id, PlaceRequestCommand command, CancellationToken ct)
    {
        var request = await MovableRequestAsync(id, ct);
        var placement = await PlaceAsync(command.ProfessionalId, request.AppointmentTypeId, command.Start, null, request.Id, ct);
        return await unitOfWork.InTransactionAsync(async () =>
        {
            var clientId = await EnsureClientAsync(request, ct);
            var keepsCoverage = request.InsurerId is Guid insurerId && await AcceptsInsurerAsync(command.ProfessionalId, insurerId, ct);
            var appointment = await appointments.BookAsync(new Appointment
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ProfessionalId = command.ProfessionalId,
                LocationId = placement.LocationId,
                RoomId = placement.RoomId,
                EquipmentId = placement.EquipmentId,
                AppointmentTypeId = request.AppointmentTypeId,
                Start = command.Start,
                End = placement.End,
                Status = AppointmentStatus.Booked,
                InsurerId = keepsCoverage ? request.InsurerId : null,
                MemberNumber = keepsCoverage ? request.MemberNumber : null,
                IsOverbook = placement.Overbook
            }, ct);
            if (request.Start != command.Start)
                request.PreviousStart ??= request.Start;
            request.Start = command.Start;
            request.End = placement.End;
            request.ProfessionalId = command.ProfessionalId;
            if (request.Status == BookingRequestStatus.Pending && request.PreviousStart is null)
                request.Status = BookingRequestStatus.Approved;
            else
                request.Status = BookingRequestStatus.Rescheduled;
            request.ClientId = clientId;
            request.AppointmentId = appointment.Id;
            await requests.SaveAsync(ct);
            await audit.RecordAsync(currentUser.Id, "alta", "Turno", appointment.Id.ToString(), request.Name, ct);
            return await LoadAppointmentAsync(appointment.Id, ct);
        }, ct);
    }

    public async Task<AppointmentDto> EditOnAgendaAsync(Guid id, EditAppointmentCommand command, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            throw new BusinessRuleException("Ese turno ya no se puede editar.");
        var (insurerId, memberNumber) = await CoverageAsync(appointment.ProfessionalId, command.InsurerId, command.MemberNumber, ct);
        if (insurerId != appointment.InsurerId)
            appointment.CoverageAccepted = null;
        appointment.InsurerId = insurerId;
        appointment.MemberNumber = memberNumber;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "edicion", "Turno", appointment.Id.ToString(), appointment.Start.ToString("O"), ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    // The one check every agenda write goes through. requestId is the public request being placed, whose own hold must not block it.
    // roomId puts the appointment in that room instead of the one its block uses.
    private async Task<Placement> PlaceAsync(Guid professionalId, Guid appointmentTypeId, DateTimeOffset start, Guid? movingId, Guid? requestId, CancellationToken ct, Guid? roomId = null)
    {
        if (start <= clock.GetUtcNow())
            throw new BusinessRuleException("No se puede dar un turno en un horario que ya pasó.");
        var professional = await employees.Employees
            .Where(e => e.UserId == professionalId && e.IsActive && e.User.Role == TenantRole.Doctor)
            .Select(e => new { e.SpecialtyId })
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Ese profesional no está activo.");
        var type = await organization.AppointmentTypes
            .Where(t => t.Id == appointmentTypeId)
            .Select(t => new { t.SpecialtyId, t.DurationMinutes, t.EquipmentId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Esa prestación no existe.");
        if (type.SpecialtyId != professional.SpecialtyId)
            throw new BusinessRuleException("Ese profesional no atiende esa prestación.");

        var candidate = new TimeRange(start, start.AddMinutes(type.DurationMinutes));
        var blockedOut = await schedules.Blockouts
            .AnyAsync(b => b.ProfessionalId == professionalId && b.Start < candidate.End && b.End > candidate.Start, ct);
        if (blockedOut)
            throw new ConflictException("El profesional tiene un bloqueo en ese horario.");
        var busy = await appointments.Appointments
            .Where(a => a.ProfessionalId == professionalId && a.Id != movingId && a.Start < candidate.End && a.End > candidate.Start)
            .Select(a => new { a.Start, a.End, a.Status })
            .ToListAsync(ct);
        if (SchedulingRules.HasOverlap(busy.Select(a => (new TimeRange(a.Start, a.End), a.Status)), candidate))
            throw new ConflictException("Se superpone con otro turno de ese profesional.");
        var holdsSince = SchedulingRules.HoldsSince(clock.GetUtcNow());
        var held = await requests.Entries
            .Where(r => r.ProfessionalId == professionalId && r.Id != requestId && r.Start < candidate.End && r.End > candidate.Start)
            .Where(r => r.Status == BookingRequestStatus.Pending || (r.Status == BookingRequestStatus.Rescheduled && r.AppointmentId == null))
            .Where(r => r.UpdatedAt >= holdsSince)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(ct);
        if (held is not null)
            throw new ConflictException($"Ese horario lo pidió {held} y está esperando confirmación. Ubicá o rechazá esa solicitud primero.");

        var date = SchedulingRules.LocalDate(start, timeZone.Zone);
        var blocks = await schedules.Blocks
            .Where(b => b.ProfessionalId == professionalId && b.Day == date.DayOfWeek)
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync(ct);
        var agenda = blocks.Select(b => new TimeRange(SchedulingRules.Combine(date, b.StartTime, timeZone.Zone), SchedulingRules.Combine(date, b.EndTime, timeZone.Zone)));
        var overbook = SchedulingRules.IsOverbook(agenda, candidate);
        if (overbook)
            await CheckOverbookLimitAsync(professionalId, date, movingId, ct);

        var (location, room) = await SiteAsync(professionalId, start, ct);
        if (roomId is Guid chosen)
        {
            location = await organization.Rooms
                .Where(r => r.Id == chosen && r.IsActive)
                .Select(r => (Guid?)r.LocationId)
                .FirstOrDefaultAsync(ct)
                ?? throw new BusinessRuleException("Ese consultorio no está activo.");
            room = chosen;
        }
        await CheckResourcesAsync(room, type.EquipmentId, candidate, movingId, ct);
        return new Placement(candidate.End, location, room, type.EquipmentId, overbook);
    }

    // The site and room of the block the moment falls in, or of the closest block that day for an overbook,
    // not just the first block of the day; with no block at all, the first active site and no room.
    private async Task<(Guid LocationId, Guid? RoomId)> SiteAsync(Guid professionalId, DateTimeOffset start, CancellationToken ct)
    {
        var date = SchedulingRules.LocalDate(start, timeZone.Zone);
        var local = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, timeZone.Zone).DateTime);
        var blocks = await schedules.Blocks
            .Where(b => b.ProfessionalId == professionalId && b.Day == date.DayOfWeek)
            .Select(b => new { b.StartTime, b.EndTime, b.LocationId, b.RoomId })
            .ToListAsync(ct);
        var block = blocks
            .OrderBy(b => b.StartTime <= local && local < b.EndTime ? 0 : 1)
            .ThenBy(b => Math.Min(Math.Abs((b.StartTime - local).TotalMinutes), Math.Abs((b.EndTime - local).TotalMinutes)))
            .FirstOrDefault();
        if (block is not null)
            return (block.LocationId, block.RoomId);
        var location = await organization.Locations.Where(l => l.IsActive).Select(l => (Guid?)l.Id).FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("No hay una sede activa.");
        return (location, null);
    }

    // A room or a piece of equipment holds one appointment at a time, whoever the professional is.
    private async Task CheckResourcesAsync(Guid? roomId, Guid? equipmentId, TimeRange candidate, Guid? movingId, CancellationToken ct)
    {
        var overlapping = appointments.Appointments
            .Where(a => a.Id != movingId && a.Start < candidate.End && a.End > candidate.Start)
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow);
        if (roomId is Guid room)
        {
            var clash = await overlapping
                .Where(a => a.RoomId == room)
                .Select(a => new { Room = a.Room!.Name, Professional = (a.Professional.FirstName + " " + a.Professional.LastName).Trim() })
                .FirstOrDefaultAsync(ct);
            if (clash is not null)
                throw new ConflictException($"El consultorio {clash.Room} está ocupado a esa hora por un turno de {clash.Professional}.");
        }
        if (equipmentId is Guid equipment)
        {
            var clash = await overlapping
                .Where(a => a.EquipmentId == equipment)
                .Select(a => new { Equipment = a.Equipment!.Name, Professional = (a.Professional.FirstName + " " + a.Professional.LastName).Trim() })
                .FirstOrDefaultAsync(ct);
            if (clash is not null)
                throw new ConflictException($"El equipo {clash.Equipment} está en uso a esa hora en un turno de {clash.Professional}.");
        }
    }

    private async Task CheckOverbookLimitAsync(Guid professionalId, DateOnly date, Guid? movingId, CancellationToken ct)
    {
        var max = (await clinicSettings.GetAsync(ct)).MaxOverbooksPerDay;
        if (max is null)
            return;
        if (max == 0)
            throw new BusinessRuleException("La clínica no da sobreturnos. Elegí un horario dentro de la agenda del profesional.");
        var dayStart = SchedulingRules.Combine(date, TimeOnly.MinValue, timeZone.Zone);
        var dayEnd = dayStart.AddDays(1);
        var already = await appointments.Appointments
            .Where(a => a.ProfessionalId == professionalId && a.IsOverbook && a.Id != movingId && a.Start >= dayStart && a.Start < dayEnd)
            .CountAsync(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow, ct);
        if (!SchedulingRules.OverbookAllowed(max, already))
            throw new BusinessRuleException($"Ese profesional ya tiene {already} {(already == 1 ? "sobreturno" : "sobreturnos")} ese día, el máximo que permite la clínica.");
    }

    private async Task<(Guid? RoomId, Guid? EquipmentId)> ResourcesAsync(Guid professionalId, Guid appointmentTypeId, DateTimeOffset start, CancellationToken ct)
    {
        var (_, room) = await SiteAsync(professionalId, start, ct);
        var equipment = await organization.AppointmentTypes.Where(t => t.Id == appointmentTypeId).Select(t => t.EquipmentId).FirstOrDefaultAsync(ct);
        return (room, equipment);
    }

    private Task<bool> AcceptsInsurerAsync(Guid professionalId, Guid insurerId, CancellationToken ct) =>
        employees.Employees
            .Where(e => e.UserId == professionalId && e.IsActive && e.User.Role == TenantRole.Doctor)
            .SelectMany(e => e.Insurers)
            .AnyAsync(i => i.Id == insurerId && i.Enabled, ct);

    private async Task<(Guid? InsurerId, string? MemberNumber)> CoverageAsync(Guid professionalId, Guid? insurerId, string? memberNumber, CancellationToken ct)
    {
        if (insurerId is not Guid chosen)
        {
            if (!string.IsNullOrWhiteSpace(memberNumber))
                throw new BusinessRuleException("El número de afiliado es para cuando elegís obra social.");
            return (null, null);
        }
        var member = memberNumber?.Trim() ?? "";
        if (member.Length is 0 or > 40)
            throw new BusinessRuleException("Si usás obra social, el número de afiliado es obligatorio.");
        if (!await AcceptsInsurerAsync(professionalId, chosen, ct))
            throw new BusinessRuleException("Ese profesional no atiende por esa obra social.");
        return (chosen, member);
    }

    // A new patient's data is only loaded once front desk told them the privacy notice.
    private async Task<Guid> ClientForDocumentAsync(string documentNumber, string? firstName, string? lastName, string? phone, bool patientInformed, CancellationToken ct)
    {
        var document = DocumentRules.Normalize(documentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        if (await FindClientIdAsync(document, ct) is Guid found)
            return found;

        var first = firstName?.Trim() ?? "";
        var last = lastName?.Trim() ?? "";
        if (first.Length is 0 or > 80 || last.Length is 0 or > 80)
            throw new BusinessRuleException("No hay un paciente con ese documento: cargá nombre y apellido.");
        PrivacyService.RequireInformed(patientInformed);
        var client = await clients.AddAsync(new Client
        {
            Id = Guid.NewGuid(),
            FirstName = first,
            LastName = last,
            DocumentNumber = document,
            Phone = PhoneRules.Require(phone)
        }, ct);
        records.Add(new MedicalRecord
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            UpdatedAt = clock.GetUtcNow(),
            UpdatedBy = currentUser.Id
        });
        await records.SaveAsync(ct);
        await privacy.RecordAsync(document, client.Id, ConsentChannel.FrontDesk, ct);
        return client.Id;
    }
}
