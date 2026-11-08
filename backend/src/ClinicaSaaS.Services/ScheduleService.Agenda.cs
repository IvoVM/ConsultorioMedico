using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public partial class ScheduleService
{
    private readonly record struct Placement(DateTimeOffset End, Guid LocationId, bool Overbook);

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
            .Select(t => new { t.SpecialtyId, Type = new AgendaTypeDto(t.Id, t.Name, t.DurationMinutes) })
            .ToListAsync(ct);
        var shown = await appointments.Appointments
            .Where(a => ids.Contains(a.ProfessionalId) && a.Start >= dayStart && a.Start < dayEnd && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.Start)
            .ToAppointmentDtos()
            .ToListAsync(ct);

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
            types.Where(t => t.SpecialtyId == p.SpecialtyId).Select(t => t.Type).ToList(),
            p.Insurers)).ToList();

        return new AgendaDayDto(date, !doctor, professionals, shown);
    }

    public async Task<AppointmentDto> MoveOnAgendaAsync(Guid id, MoveAppointmentCommand command, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (!SchedulingRules.CanMoveOnAgenda(appointment.Status))
            throw new BusinessRuleException("Ese turno ya no se puede mover.");
        var placement = await PlaceAsync(command.ProfessionalId, appointment.AppointmentTypeId, command.Start, appointment.Id, ct);
        appointment.ProfessionalId = command.ProfessionalId;
        appointment.LocationId = placement.LocationId;
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
    }

    public async Task<AppointmentDto> BookOnAgendaAsync(AgendaBookCommand command, CancellationToken ct)
    {
        var placement = await PlaceAsync(command.ProfessionalId, command.AppointmentTypeId, command.Start, null, ct);
        var (insurerId, memberNumber) = await CoverageAsync(command.ProfessionalId, command.InsurerId, command.MemberNumber, ct);
        var clientId = await ClientForDocumentAsync(command.DocumentNumber, command.FirstName, command.LastName, command.Phone, ct);
        var reason = command.VisitReason?.Trim();
        if (reason is { Length: > 500 })
            throw new BusinessRuleException("El motivo tiene que tener menos de 500 caracteres.");
        var appointment = await appointments.BookAsync(new Appointment
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = command.ProfessionalId,
            LocationId = placement.LocationId,
            AppointmentTypeId = command.AppointmentTypeId,
            Start = command.Start,
            End = placement.End,
            Status = AppointmentStatus.Booked,
            VisitReason = string.IsNullOrEmpty(reason) ? null : reason,
            InsurerId = insurerId,
            MemberNumber = memberNumber,
            IsOverbook = placement.Overbook
        }, ct);
        await audit.RecordAsync(currentUser.Id, placement.Overbook ? "sobreturno" : "reserva", "Turno", appointment.Id.ToString(), appointment.Start.ToString("O"), ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    public async Task<AppointmentDto> PlaceRequestOnAgendaAsync(Guid id, PlaceRequestCommand command, CancellationToken ct)
    {
        var request = await MovableRequestAsync(id, ct);
        var placement = await PlaceAsync(command.ProfessionalId, request.AppointmentTypeId, command.Start, null, ct);
        var clientId = await EnsureClientAsync(request, ct);
        var keepsCoverage = request.InsurerId is Guid insurerId && await AcceptsInsurerAsync(command.ProfessionalId, insurerId, ct);
        var appointment = await appointments.BookAsync(new Appointment
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = command.ProfessionalId,
            LocationId = placement.LocationId,
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
    }

    public async Task<AppointmentDto> EditOnAgendaAsync(Guid id, EditAppointmentCommand command, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(id, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            throw new BusinessRuleException("Ese turno ya no se puede editar.");
        var reason = command.VisitReason?.Trim();
        if (reason is { Length: > 500 })
            throw new BusinessRuleException("El motivo tiene que tener menos de 500 caracteres.");
        var (insurerId, memberNumber) = await CoverageAsync(appointment.ProfessionalId, command.InsurerId, command.MemberNumber, ct);
        if (insurerId != appointment.InsurerId)
            appointment.CoverageAccepted = null;
        appointment.VisitReason = string.IsNullOrEmpty(reason) ? null : reason;
        appointment.InsurerId = insurerId;
        appointment.MemberNumber = memberNumber;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "edicion", "Turno", appointment.Id.ToString(), appointment.Start.ToString("O"), ct);
        return await LoadAppointmentAsync(appointment.Id, ct);
    }

    private async Task<Placement> PlaceAsync(Guid professionalId, Guid appointmentTypeId, DateTimeOffset start, Guid? movingId, CancellationToken ct)
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
            .Select(t => new { t.SpecialtyId, t.DurationMinutes })
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

        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, timeZone.Zone).DateTime);
        var blocks = await schedules.Blocks
            .Where(b => b.ProfessionalId == professionalId && b.Day == date.DayOfWeek)
            .Select(b => new { b.StartTime, b.EndTime, b.LocationId })
            .ToListAsync(ct);
        var agenda = blocks.Select(b => new TimeRange(SchedulingRules.Combine(date, b.StartTime, timeZone.Zone), SchedulingRules.Combine(date, b.EndTime, timeZone.Zone)));
        var location = blocks.Select(b => (Guid?)b.LocationId).FirstOrDefault()
            ?? await organization.Locations.Where(l => l.IsActive).Select(l => (Guid?)l.Id).FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("No hay una sede activa.");
        return new Placement(candidate.End, location, SchedulingRules.IsOverbook(agenda, candidate));
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

    private async Task<Guid> ClientForDocumentAsync(string documentNumber, string? firstName, string? lastName, string? phone, CancellationToken ct)
    {
        var document = DocumentRules.Normalize(documentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        var known = await clients.Clients
            .Where(client => client.DocumentNumber == document || client.DocumentNumber.ToLower() == document.ToLower())
            .Select(client => (Guid?)client.Id)
            .FirstOrDefaultAsync(ct);
        if (known is Guid found)
            return found;

        var first = firstName?.Trim() ?? "";
        var last = lastName?.Trim() ?? "";
        if (first.Length is 0 or > 80 || last.Length is 0 or > 80)
            throw new BusinessRuleException("No hay un paciente con ese documento: cargá nombre y apellido.");
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
        return client.Id;
    }
}
