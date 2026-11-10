using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.MappingExtensions;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The change trail of each appointment. Everyone reads only what is theirs; only the admin writes over it.
public partial class ScheduleService
{
    public async Task<IReadOnlyList<AppointmentChangeDto>> ChangesAsync(Guid id, CancellationToken ct)
    {
        var readable = await ReadableAppointmentsAsync(ct);
        if (!await readable.AnyAsync(a => a.Id == id, ct))
            throw new NotFoundException("Turno no encontrado.");
        return await TrailAsync(id, ct);
    }

    public async Task<IReadOnlyList<AppointmentChangeDto>> AddChangeAsync(Guid appointmentId, SaveAppointmentChangeCommand command, CancellationToken ct)
    {
        await CheckChangeAsync(appointmentId, command, ct);
        appointments.AddChange(new AppointmentChange
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Status = command.Status,
            Start = command.Start,
            End = command.End,
            ProfessionalId = command.ProfessionalId,
            CreatedAt = command.At,
        });
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "historial-alta", "Turno", appointmentId.ToString(), null, ct);
        return await TrailAsync(appointmentId, ct);
    }

    public async Task<IReadOnlyList<AppointmentChangeDto>> UpdateChangeAsync(Guid appointmentId, Guid changeId, SaveAppointmentChangeCommand command, CancellationToken ct)
    {
        await CheckChangeAsync(appointmentId, command, ct);
        var change = await appointments.GetChangeAsync(appointmentId, changeId, ct)
            ?? throw new NotFoundException("Movimiento no encontrado.");
        change.Status = command.Status;
        change.Start = command.Start;
        change.End = command.End;
        change.ProfessionalId = command.ProfessionalId;
        change.CreatedAt = command.At;
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "historial-edicion", "Turno", appointmentId.ToString(), changeId.ToString(), ct);
        return await TrailAsync(appointmentId, ct);
    }

    public async Task<IReadOnlyList<AppointmentChangeDto>> DeleteChangeAsync(Guid appointmentId, Guid changeId, CancellationToken ct)
    {
        var change = await appointments.GetChangeAsync(appointmentId, changeId, ct)
            ?? throw new NotFoundException("Movimiento no encontrado.");
        appointments.RemoveChange(change);
        await appointments.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "historial-baja", "Turno", appointmentId.ToString(), changeId.ToString(), ct);
        return await TrailAsync(appointmentId, ct);
    }

    // Front desk and admin read every appointment; a professional only the ones they attend; a patient only their own.
    private async Task<IQueryable<Appointment>> ReadableAppointmentsAsync(CancellationToken ct)
    {
        var source = appointments.Appointments;
        if (currentUser.Role == TenantRole.Doctor.ToString())
            return source.Where(a => a.ProfessionalId == (currentUser.Id ?? Guid.Empty));
        if (currentUser.Role == TenantRole.Patient.ToString())
        {
            var clientId = await clients.Clients
                .Where(c => c.UserId == (currentUser.Id ?? Guid.Empty))
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
            return source.Where(a => a.ClientId == clientId);
        }
        return source;
    }

    private async Task CheckChangeAsync(Guid appointmentId, SaveAppointmentChangeCommand command, CancellationToken ct)
    {
        if (!await appointments.Appointments.AnyAsync(a => a.Id == appointmentId, ct))
            throw new NotFoundException("Turno no encontrado.");
        if (command.End <= command.Start)
            throw new BusinessRuleException("El fin del turno debe ser posterior al inicio.");
        if (!await employees.Employees.AnyAsync(e => e.UserId == command.ProfessionalId, ct))
            throw new NotFoundException("Profesional no encontrado.");
    }

    private async Task<IReadOnlyList<AppointmentChangeDto>> TrailAsync(Guid appointmentId, CancellationToken ct) =>
        await appointments.Changes
            .Where(c => c.AppointmentId == appointmentId)
            .OrderBy(c => c.CreatedAt)
            .ToAppointmentChangeDtos()
            .ToListAsync(ct);
}
