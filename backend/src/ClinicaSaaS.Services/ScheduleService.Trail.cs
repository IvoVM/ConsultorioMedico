using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.MappingExtensions;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The change trail of each appointment. Everyone reads only what is theirs; nobody writes over it: each step is
// recorded when the appointment changes and the database refuses to edit or delete it.
public partial class ScheduleService
{
    public async Task<IReadOnlyList<AppointmentChangeDto>> ChangesAsync(Guid id, CancellationToken ct)
    {
        var readable = await ReadableAppointmentsAsync(ct);
        if (!await readable.AnyAsync(a => a.Id == id, ct))
            throw new NotFoundException("Turno no encontrado.");
        return await TrailAsync(id, ct);
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

    private async Task<IReadOnlyList<AppointmentChangeDto>> TrailAsync(Guid appointmentId, CancellationToken ct) =>
        await appointments.Changes
            .Where(c => c.AppointmentId == appointmentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToAppointmentChangeDtos()
            .ToListAsync(ct);
}
