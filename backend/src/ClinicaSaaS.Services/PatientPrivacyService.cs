using System.Text.Json;
using System.Text.Json.Serialization;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The patient's rights over their data (Ley 25.326, art. 14 to 16) from their own account: see what they agreed to,
// download everything the clinic keeps about them, and close the account. The clinical record and the invoices are
// not deleted: the law keeps the record for ten years (Ley 26.529, art. 18) and the invoices for the tax authority.
public class PatientPrivacyService(
    IClientStore clients,
    IMedicalRecordStore records,
    IConsentStore consents,
    IAppointmentStore appointments,
    IBookingRequestStore requests,
    IWaitlistStore waitlist,
    ITenantUserStore users,
    IRefreshSessionStore sessions,
    ICurrentUser currentUser,
    IAuditStore audit,
    PrivacyService privacy,
    ScheduleService schedule,
    InvoiceService invoices,
    ClinicalService clinical,
    TimeProvider clock)
{
    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // An appointment still to happen: closing the account would leave it with nobody to tell.
    private static readonly AppointmentStatus[] Upcoming =
        [AppointmentStatus.Booked, AppointmentStatus.CheckedIn, AppointmentStatus.InProgress, AppointmentStatus.Rescheduled];

    public async Task<PrivacyCenterDto> CenterAsync(CancellationToken ct)
    {
        var client = await OwnClientAsync(ct);
        var given = await ConsentsOfAsync(client, ct);
        return new PrivacyCenterDto(
            PrivacyService.NoticeVersion,
            given.Any(c => c.Kind == ConsentKind.PrivacyNotice && c.NoticeVersion == PrivacyService.NoticeVersion),
            given);
    }

    // For accounts created before the notice existed, or after it changed.
    public async Task<PrivacyCenterDto> AcceptAsync(AcceptPrivacyCommand command, CancellationToken ct)
    {
        PrivacyService.RequireAccepted(command.PrivacyVersion);
        var client = await OwnClientAsync(ct);
        await privacy.RecordAsync(client.DocumentNumber, client.Id, ConsentChannel.Account, ct);
        return await CenterAsync(ct);
    }

    public async Task<FileDto> ExportAsync(CancellationToken ct)
    {
        var client = await OwnClientAsync(ct);
        var file = await records.GetByClientAsync(client.Id, ct);
        var export = new
        {
            exportedAt = clock.GetUtcNow(),
            note = "La historia clínica completa se descarga en PDF desde Historia clínica.",
            profile = new
            {
                client.FirstName,
                client.LastName,
                client.DocumentNumber,
                birthDate = client.BirthDate.Year > 1 ? client.BirthDate : (DateOnly?)null,
                client.Phone,
                client.HistoryNumber
            },
            administrativeFile = file is null ? null : new
            {
                file.HealthInsurance,
                file.MemberNumber,
                file.EmergencyContact,
                file.EmergencyPhone,
                file.EmergencyContactAgreedAt
            },
            consents = await ConsentsOfAsync(client, ct),
            appointments = await schedule.MineAsync(ct),
            requests = await schedule.MyRequestsAsync(ct),
            waitlist = await waitlist.Entries
                .Where(w => w.ClientId == client.Id)
                .Select(w => new { w.CreatedAt, Specialty = w.Specialty.Name, Location = w.Location.Name, w.Status })
                .ToListAsync(ct),
            invoices = await invoices.MineAsync(ct),
            clinicalRecordAccesses = await clinical.MyAccessesAsync(ct)
        };
        await audit.RecordAsync(currentUser.Id, "exportacion", "Paciente", client.Id.ToString(), null, ct);
        return new FileDto(JsonSerializer.SerializeToUtf8Bytes(export, ExportJson), "application/json", $"mis-datos-{client.DocumentNumber}.json");
    }

    // Closes the sign-in and deletes what only served the account: requests that never became an appointment and
    // the waitlist. The patient's file, appointments, record and invoices stay, as the law requires.
    public async Task CloseAccountAsync(CloseAccountCommand command, CancellationToken ct)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        if (!await users.CheckPasswordAsync(userId, command.Password ?? "", ct))
            throw new BusinessRuleException("La contraseña no es correcta.");
        var owned = await OwnClientAsync(ct);
        var now = clock.GetUtcNow();
        if (await appointments.Appointments.AnyAsync(a => a.ClientId == owned.Id && a.Start > now && Upcoming.Contains(a.Status), ct))
            throw new BusinessRuleException("Tenés turnos por venir. Cancelalos antes de cerrar la cuenta.");

        var removedRequests = await requests.RemoveOpenAsync(owned.Id, owned.DocumentNumber, ct);
        var removedWaitlist = await waitlist.RemoveForClientAsync(owned.Id, ct);
        var client = await clients.GetAsync(owned.Id, ct) ?? throw new NotFoundException("No hay una ficha para esta cuenta.");
        client.UserId = null;
        await clients.SaveAsync(ct);
        await users.CloseAsync(userId, ct);
        await sessions.RevokeUserAsync(userId, ct);
        await audit.RecordAsync(userId, "baja de cuenta", "Paciente", client.Id.ToString(), $"{removedRequests} solicitudes y {removedWaitlist} esperas borradas", ct);
    }

    private async Task<List<ConsentDto>> ConsentsOfAsync(Client client, CancellationToken ct) =>
        await consents.Consents
            .Where(c => c.ClientId == client.Id || c.DocumentNumber == client.DocumentNumber)
            .OrderByDescending(c => c.At)
            .Select(c => new ConsentDto(c.Kind, c.Channel, c.NoticeVersion, c.At))
            .ToListAsync(ct);

    private async Task<Client> OwnClientAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedException("La sesión expiró. Volvé a ingresar.");
        return await clients.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new NotFoundException("No hay una ficha para esta cuenta.");
    }
}
