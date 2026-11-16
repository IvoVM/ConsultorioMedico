using System.Text.RegularExpressions;
using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
using ClinicaSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The clinical record under Ley 26.529 and Ley 27.706. A visit is a draft while the professional writes it; once
// signed it becomes a numbered page (folio) that nobody can edit or delete, only amend with a new signed page.
// Background facts and written consents are signed pages too. Only professionals and the patient reach the
// record, and every time someone opens, copies or signs it is logged for the patient to see.
public partial class ClinicalService(
    IClinicalStore clinical,
    IAppointmentStore appointments,
    IClientStore clients,
    InvoiceService invoices,
    SupplyService supplies,
    IOrganizationStore organization,
    IEmployeeStore employees,
    ITenantUserStore users,
    IClinicSettingsStore settings,
    ITimeZoneProvider timeZone,
    ICurrentUser currentUser,
    IAuditStore audit,
    IValidator<SaveEncounterCommand> encounterValidator,
    IValidator<AmendEntryCommand> amendValidator,
    IValidator<AddBackgroundCommand> backgroundValidator,
    IValidator<AddConsentCommand> consentValidator,
    TimeProvider clock)
{
    private const string NoSpecialty = "Sin especialidad";

    public async Task<IReadOnlyList<DiagnosisDto>> DiagnosesAsync(string? query, CancellationToken ct)
    {
        var source = clinical.Diagnoses;
        var text = query?.Trim() ?? "";
        if (text.Length > 0)
        {
            var lower = text.ToLower();
            var code = text.ToUpperInvariant().Replace(".", "");
            source = source.Where(d => d.Code.Replace(".", "").StartsWith(code) || d.Name.ToLower().Contains(lower));
        }
        return await source.OrderBy(d => d.Code).Take(30).ToDiagnosisDtos().ToListAsync(ct);
    }

    // Loads the official CIE-10 catalog the clinic is licensed to use. Lines that do not start with a code
    // (a header, blank lines) are skipped.
    public async Task<ImportedDiagnosesDto> ImportDiagnosesAsync(ImportDiagnosesCommand command, CancellationToken ct)
    {
        var rows = new Dictionary<string, string>();
        var read = 0;
        foreach (var raw in (command.Csv ?? "").Split('\n'))
        {
            var line = raw.Trim().Trim('﻿');
            if (line.Length == 0)
                continue;
            var cut = line.IndexOfAny([';', '\t', ',']);
            if (cut <= 0)
                continue;
            var code = NormalizeCode(line[..cut]);
            var name = line[(cut + 1)..].Trim().Trim('"').Trim();
            if (code is null || name.Length == 0)
                continue;
            read++;
            rows[code] = name.Length > 300 ? name[..300] : name;
        }
        if (rows.Count == 0)
            throw new BusinessRuleException("No encontré diagnósticos en el archivo. Cada línea va con el código y el nombre separados por punto y coma.");
        var added = await clinical.ImportDiagnosesAsync([.. rows.Select(r => (r.Key, r.Value))], ct);
        await audit.RecordAsync(currentUser.Id, "catalogo", "Diagnostico", null, $"{read} leídos, {added} nuevos", ct);
        return new ImportedDiagnosesDto(read, added, await clinical.Diagnoses.CountAsync(ct));
    }

    // "J069", "j06.9" and "J06.9" are the same code; anything that is not a CIE-10 code gives null.
    private static string? NormalizeCode(string raw)
    {
        var code = raw.Trim().Trim('"').ToUpperInvariant().Replace(".", "");
        if (!CieCode().IsMatch(code))
            return null;
        return code.Length > 3 ? $"{code[..3]}.{code[3..]}" : code;
    }

    [GeneratedRegex("^[A-Z][0-9]{2}[0-9A-Z]{0,4}$")]
    private static partial Regex CieCode();

    // The draft the professional left for this appointment, so reopening it does not start blank.
    public async Task<EncounterDto?> EncounterForAppointmentAsync(Guid appointmentId, CancellationToken ct)
    {
        var appointment = await appointments.GetAsync(appointmentId, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.ProfessionalId != currentUser.Id)
            throw new BusinessRuleException("Solo el profesional del turno puede abrir la consulta.");
        return await clinical.Encounters.Where(e => e.AppointmentId == appointmentId).ToEncounterDtos().FirstOrDefaultAsync(ct);
    }

    // Visits the professional started and has not signed: they are not in the record until they are.
    public async Task<IReadOnlyList<EncounterDto>> PendingEncountersAsync(CancellationToken ct) =>
        await clinical.Encounters
            .Where(e => e.ProfessionalId == currentUser.Id && !e.IsClosed)
            .OrderBy(e => e.Appointment.Start)
            .ToEncounterDtos()
            .ToListAsync(ct);

    public async Task<EncounterDto> SaveEncounterAsync(SaveEncounterCommand command, CancellationToken ct)
    {
        var result = await encounterValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var appointment = await appointments.GetAsync(command.AppointmentId, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (appointment.ProfessionalId != currentUser.Id)
            throw new BusinessRuleException("Solo el profesional del turno puede cargar la consulta.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Completed)
            throw new BusinessRuleException("Ese turno ya no se puede atender.");

        var encounter = await clinical.GetEncounterByAppointmentAsync(appointment.Id, ct);
        if (encounter is null)
        {
            encounter = new Encounter
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                ClientId = appointment.ClientId,
                ProfessionalId = appointment.ProfessionalId,
                SpecialtyId = await SpecialtyForAsync(appointment, ct),
                CreatedAt = clock.GetUtcNow()
            };
            encounter = await clinical.AddEncounterAsync(encounter, ct);
        }
        else if (encounter.IsClosed)
        {
            throw new BusinessRuleException("Esta consulta ya está firmada. Para cambiar algo, agregá una corrección desde la historia.");
        }

        encounter.Note = Clean(command.Note);
        encounter.Plan = Clean(command.Plan);
        encounter.BloodPressure = Clean(command.BloodPressure);
        encounter.HeartRate = command.HeartRate;
        encounter.Temperature = command.Temperature;
        encounter.WeightKg = command.WeightKg;
        await clinical.ReplaceDiagnosesAsync(encounter.Id, command.DiagnosisIds, ct);
        if (appointment.Status is AppointmentStatus.Booked or AppointmentStatus.Rescheduled or AppointmentStatus.CheckedIn)
            appointment.Status = AppointmentStatus.InProgress;
        await appointments.SaveAsync(ct);
        await clinical.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "encuentro", "Encuentro", encounter.Id.ToString(), null, ct);
        return await LoadEncounterAsync(encounter.Id, ct);
    }

    // Signing turns the draft into the next folio of the record, closes the visit and sends it to billing.
    public async Task<ClinicalEntryDto> SignEncounterAsync(Guid id, SignCommand command, CancellationToken ct)
    {
        var encounter = await clinical.GetEncounterAsync(id, ct) ?? throw new NotFoundException("Consulta no encontrada.");
        if (encounter.ProfessionalId != currentUser.Id)
            throw new BusinessRuleException("Solo el profesional de la consulta puede firmarla.");
        if (encounter.IsClosed)
            throw new BusinessRuleException("Esta consulta ya está firmada.");
        if (!EncounterRules.CanSign(encounter.Note))
            throw new BusinessRuleException("Para firmar la consulta hace falta la evolución (la nota clínica).");
        var signer = await SignerAsync(command.Password, ct);
        var appointment = await appointments.GetAsync(encounter.AppointmentId, ct) ?? throw new NotFoundException("Turno no encontrado.");

        var diagnoses = await clinical.Encounters
            .Where(e => e.Id == id)
            .SelectMany(e => e.Diagnoses)
            .OrderBy(d => d.Diagnosis!.Code)
            .Select(d => d.Diagnosis!.Code + " " + d.Diagnosis.Name)
            .ToListAsync(ct);
        var entry = NewEntry(ClinicalEntryKind.Visit, encounter.ClientId, signer);
        entry.EncounterId = encounter.Id;
        entry.SpecialtyId = encounter.SpecialtyId;
        entry.SpecialtyName = await SpecialtyNameAsync(encounter.SpecialtyId, ct);
        entry.Text = encounter.Note!;
        entry.Plan = encounter.Plan;
        entry.BloodPressure = encounter.BloodPressure;
        entry.HeartRate = encounter.HeartRate;
        entry.Temperature = encounter.Temperature;
        entry.WeightKg = encounter.WeightKg;
        entry.Diagnoses = diagnoses.Count > 0 ? string.Join("; ", diagnoses) : null;

        encounter.IsClosed = true;
        appointment.Status = AppointmentStatus.Completed;
        await clinical.AppendAsync(entry, null, ct);
        await supplies.ConsumeForAsync(appointment.Id, appointment.AppointmentTypeId, ct);
        await invoices.IssueOnCloseAsync(appointment, ct);
        await LogAsync(entry.ClientId, ClinicalAccessAction.Sign, $"Folio {entry.Folio}", ct);
        await audit.RecordAsync(currentUser.Id, "firma", "Encuentro", encounter.Id.ToString(), $"Folio {entry.Folio}", ct);
        return ToDto(entry, null, false);
    }

    // A signed page is never changed: the correction is a new signed page that points to it.
    public async Task<ClinicalEntryDto> AmendAsync(Guid entryId, AmendEntryCommand command, CancellationToken ct)
    {
        var result = await amendValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        var original = await clinical.Entries.FirstOrDefaultAsync(e => e.Id == entryId && e.Hash != null, ct)
            ?? throw new NotFoundException("Folio no encontrado.");
        if (original.Kind == ClinicalEntryKind.Amendment)
            throw new BusinessRuleException("Agregá la corrección sobre el folio original, no sobre otra corrección.");
        var signer = await SignerAsync(command.Password, ct);
        if (!CanAmend(original, signer.Id, signer.SpecialtyId))
            throw new BusinessRuleException("Solo quien firmó ese folio puede corregirlo.");

        var entry = NewEntry(ClinicalEntryKind.Amendment, original.ClientId, signer);
        entry.AmendsId = original.Id;
        entry.SpecialtyId = original.SpecialtyId;
        entry.SpecialtyName = original.SpecialtyName;
        entry.BackgroundKind = original.BackgroundKind;
        entry.Text = command.Text.Trim();
        await clinical.AppendAsync(entry, null, ct);
        await LogAsync(entry.ClientId, ClinicalAccessAction.Amend, $"Folio {entry.Folio} corrige el {original.Folio}", ct);
        await audit.RecordAsync(currentUser.Id, "correccion", "Historia", entry.Id.ToString(), $"Folio {entry.Folio}", ct);
        return ToDto(entry, original.Folio, false);
    }

    public async Task<ClinicalEntryDto> AddBackgroundAsync(AddBackgroundCommand command, CancellationToken ct)
    {
        var result = await backgroundValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        await EnsurePatientExistsAsync(command.PatientId, ct);
        var signer = await SignerAsync(command.Password, ct);

        var entry = NewEntry(ClinicalEntryKind.Background, command.PatientId, signer);
        entry.SpecialtyId = signer.SpecialtyId;
        entry.SpecialtyName = signer.SpecialtyName;
        entry.BackgroundKind = command.Kind;
        entry.Text = command.Kind == BackgroundKind.BloodType ? command.Text.Trim().ToUpperInvariant() : command.Text.Trim();
        await clinical.AppendAsync(entry, null, ct);
        await LogAsync(entry.ClientId, ClinicalAccessAction.Sign, $"Folio {entry.Folio}", ct);
        await audit.RecordAsync(currentUser.Id, "antecedente", "Historia", entry.Id.ToString(), $"Folio {entry.Folio}", ct);
        return ToDto(entry, null, false);
    }

    // The patient signs the consent on paper; the professional scans it and signs the page that cites it.
    public async Task<ClinicalEntryDto> AddConsentAsync(AddConsentCommand command, CancellationToken ct)
    {
        var result = await consentValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        await EnsurePatientExistsAsync(command.PatientId, ct);
        byte[] content;
        try
        {
            content = Convert.FromBase64String(command.ContentBase64);
        }
        catch (FormatException)
        {
            throw new BusinessRuleException("No se pudo leer el archivo del consentimiento.");
        }
        if (content.Length == 0 || content.Length > ClinicalLedger.MaxAttachmentBytes)
            throw new BusinessRuleException("El consentimiento tiene que pesar hasta 5 MB.");
        if (!MatchesType(content, command.ContentType))
            throw new BusinessRuleException("El archivo no es un PDF, JPG o PNG válido.");
        var signer = await SignerAsync(command.Password, ct);

        var attachment = new ClinicalAttachment
        {
            Id = Guid.NewGuid(),
            ClientId = command.PatientId,
            Name = Path.GetFileName(command.FileName.Trim()),
            ContentType = command.ContentType,
            Content = content,
            Sha256 = ClinicalLedger.Sha256(content),
            UploadedAt = clock.GetUtcNow()
        };
        var entry = NewEntry(ClinicalEntryKind.Consent, command.PatientId, signer);
        entry.SpecialtyId = signer.SpecialtyId;
        entry.SpecialtyName = signer.SpecialtyName;
        entry.Text = command.Procedure.Trim();
        entry.ConsentDecision = command.Decision;
        entry.AttachmentId = attachment.Id;
        entry.AttachmentSha256 = attachment.Sha256;
        await clinical.AppendAsync(entry, attachment, ct);
        await LogAsync(entry.ClientId, ClinicalAccessAction.Sign, $"Folio {entry.Folio}", ct);
        await audit.RecordAsync(currentUser.Id, "consentimiento", "Historia", entry.Id.ToString(), $"Folio {entry.Folio}", ct);
        return ToDto(entry, null, false);
    }

    public async Task<FileDto> AttachmentAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await clinical.Entries.FirstOrDefaultAsync(e => e.Id == entryId && e.AttachmentId != null, ct)
            ?? throw new NotFoundException("Adjunto no encontrado.");
        await EnsureCanSeeAsync(entry, ct);
        var attachment = await clinical.GetAttachmentAsync(entry.AttachmentId!.Value, ct) ?? throw new NotFoundException("Adjunto no encontrado.");
        if (ClinicalLedger.Sha256(attachment.Content) != entry.AttachmentSha256)
            throw new BusinessRuleException("El adjunto no coincide con el que se firmó. Avisá al administrador.");
        await LogAsync(entry.ClientId, ClinicalAccessAction.Attachment, $"Folio {entry.Folio}", ct);
        return new FileDto(attachment.Content, attachment.ContentType, attachment.Name);
    }

    // The professional sees background pages and the pages of their own specialty; the patient sees all of
    // theirs, one specialty at a time.
    public async Task<ClinicalHistoryDto> HistoryAsync(Guid? patientId, Guid? specialtyId, CancellationToken ct)
    {
        var clientId = await ResolvePatientAsync(patientId, ct);
        var summary = await clients.Clients.Where(c => c.Id == clientId).ToPatientSummaries().FirstAsync(ct);
        var all = await SealedEntriesAsync(clientId, ct);

        var available = all
            .Where(e => e.Kind == ClinicalEntryKind.Visit)
            .GroupBy(e => e.SpecialtyId ?? Guid.Empty)
            .Select(g => new { Id = g.Key, Name = g.Last().SpecialtyName, Count = g.Count(), Last = g.Max(e => e.Folio) })
            .OrderByDescending(g => g.Last)
            .Select(g => new HistorySpecialtyDto(g.Id, g.Name.Length > 0 ? g.Name : NoSpecialty, g.Count))
            .ToList();

        HistorySpecialtyDto? chosen;
        Guid? own = null;
        if (IsDoctor)
        {
            var mine = await OwnSpecialtyAsync(ct);
            if (specialtyId is Guid asked && asked != mine.Id)
                throw new BusinessRuleException("Solo podés ver la historia de tu especialidad.");
            chosen = available.FirstOrDefault(s => s.Id == mine.Id) ?? mine;
            available = [chosen];
            own = mine.Id;
        }
        else
        {
            chosen = specialtyId is Guid asked
                ? available.FirstOrDefault(s => s.Id == asked) ?? throw new NotFoundException("No hay historia en esa especialidad.")
                : available.FirstOrDefault();
        }

        var folios = all.ToDictionary(e => e.Id, e => e.Folio!.Value);
        var shown = all
            .Where(e => e.BackgroundKind is not null || (chosen is not null && (e.SpecialtyId ?? Guid.Empty) == chosen.Id))
            .OrderByDescending(e => e.Folio)
            .Select(e => ToDto(e, e.AmendsId is Guid a ? folios[a] : null, own is not null && CanAmend(e, currentUser.Id, own == Guid.Empty ? null : own)))
            .ToList();

        await LogAsync(clientId, ClinicalAccessAction.View, chosen?.Name, ct);
        return new ClinicalHistoryDto(summary, chosen, available, Background(all), shown, ClinicalLedger.FirstBroken(all));
    }

    // The authenticated copy the patient can ask for at any time (Ley 26.529, art. 14), as a PDF.
    public async Task<FileDto> CopyAsync(Guid? patientId, CancellationToken ct)
    {
        var clientId = await ResolvePatientAsync(patientId, ct);
        var summary = await clients.Clients.Where(c => c.Id == clientId).ToPatientSummaries().FirstAsync(ct);
        var all = await SealedEntriesAsync(clientId, ct);
        var folios = all.ToDictionary(e => e.Id, e => e.Folio!.Value);
        IEnumerable<ClinicalEntry> visible = all;
        var scope = "Copia completa";
        if (IsDoctor)
        {
            var mine = await OwnSpecialtyAsync(ct);
            visible = all.Where(e => e.BackgroundKind is not null || (e.SpecialtyId ?? Guid.Empty) == mine.Id);
            scope = $"Antecedentes y folios de {mine.Name}";
        }
        var shown = visible.OrderBy(e => e.Folio).ToList();
        var entries = shown.Select(e => ToDto(e, e.AmendsId is Guid a ? folios[a] : null, false)).ToList();
        var signatures = await SignaturesForAsync(shown, ct);
        var clinic = await settings.GetAsync(ct);
        var issuedBy = await CurrentUserNameAsync(ct);
        var pdf = HistoryPdf.Render(
            new HistoryCopy(clinic.Name, summary, entries, signatures, ClinicalLedger.FirstBroken(all), scope, issuedBy, clock.GetUtcNow()),
            timeZone.Zone);
        await LogAsync(clientId, ClinicalAccessAction.Copy, scope, ct);
        return new FileDto(pdf, "application/pdf", $"historia-clinica-{summary.HistoryNumber}.pdf");
    }

    // Who opened, copied or wrote in the patient's own record, staff only (Ley 27.706, art. 6 d).
    public async Task<IReadOnlyList<ClinicalAccessDto>> MyAccessesAsync(CancellationToken ct)
    {
        var clientId = await ResolvePatientAsync(null, ct);
        return await clinical.Accesses
            .Where(a => a.ClientId == clientId && a.Role != TenantRole.Patient)
            .OrderByDescending(a => a.At)
            .Take(200)
            .Select(a => new ClinicalAccessDto(a.At, a.UserName, a.Role, a.Action, a.Detail))
            .ToListAsync(ct);
    }

    private bool IsDoctor => currentUser.Role == TenantRole.Doctor.ToString();

    private sealed record Signer(Guid Id, string Name, string License, Guid? SpecialtyId, string SpecialtyName, ProfessionalSignature? Signature);

    // Electronic signature: the professional's own password, typed again, and their license on file.
    private async Task<Signer> SignerAsync(string? password, CancellationToken ct)
    {
        if (!IsDoctor || currentUser.Id is not Guid id)
            throw new BusinessRuleException("Solo un profesional puede firmar en la historia clínica.");
        if (string.IsNullOrEmpty(password) || !await users.CheckPasswordAsync(id, password, ct))
            throw new BusinessRuleException("La contraseña no es correcta. No se firmó nada.");
        var employee = await employees.Employees
            .Where(e => e.UserId == id)
            .Select(e => new
            {
                e.LicenseNumber,
                e.SignatureId,
                e.SpecialtyId,
                Specialty = e.Specialty != null ? e.Specialty.Name : null,
                Name = (e.User.FirstName + " " + e.User.LastName).Trim()
            })
            .FirstOrDefaultAsync(ct);
        if (employee is null || string.IsNullOrWhiteSpace(employee.LicenseNumber))
            throw new BusinessRuleException("Para firmar hace falta tu matrícula. Pedile al administrador que la cargue en tu ficha de profesional.");
        var signature = employee.SignatureId is Guid signatureId ? await clinical.GetSignatureAsync(signatureId, ct) : null;
        return new Signer(id, employee.Name, employee.LicenseNumber.Trim(), employee.SpecialtyId, employee.Specialty ?? NoSpecialty, signature);
    }

    private ClinicalEntry NewEntry(ClinicalEntryKind kind, Guid clientId, Signer signer) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        Kind = kind,
        AuthorId = signer.Id,
        AuthorName = signer.Name,
        AuthorLicense = signer.License,
        SignatureId = signer.Signature?.Id,
        SignatureSha256 = signer.Signature?.Sha256,
        RecordedAt = clock.GetUtcNow(),
        Signed = true
    };

    // The signer amends their own page. A page brought over from before signatures was not signed by anyone,
    // so any professional who can see it may correct it, signing as themselves.
    private static bool CanAmend(ClinicalEntry entry, Guid? userId, Guid? specialtyId) =>
        entry.Kind != ClinicalEntryKind.Amendment
        && (entry.Signed
            ? entry.AuthorId == userId
            : entry.BackgroundKind is not null || entry.SpecialtyId == specialtyId);

    private static IReadOnlyList<BackgroundItemDto> Background(IReadOnlyList<ClinicalEntry> all) =>
        all.Where(e => e.BackgroundKind is not null)
            .GroupBy(e => e.BackgroundKind!.Value)
            .Select(g => g.MaxBy(e => e.Folio)!)
            .Where(e => e.Text.Length > 0)
            .OrderBy(e => e.BackgroundKind)
            .Select(e => new BackgroundItemDto(e.BackgroundKind!.Value, e.Text, e.Folio!.Value, e.RecordedAt, e.AuthorName))
            .ToList();

    private static ClinicalEntryDto ToDto(ClinicalEntry e, int? amendsFolio, bool canAmend) => new(
        e.Id,
        e.Folio ?? 0,
        e.Kind,
        e.RecordedAt,
        e.Signed,
        e.AuthorName,
        e.AuthorLicense,
        e.SpecialtyName.Length > 0 ? e.SpecialtyName : NoSpecialty,
        e.Text,
        e.Plan,
        e.BloodPressure,
        e.HeartRate,
        e.Temperature,
        e.WeightKg,
        e.Diagnoses,
        e.BackgroundKind,
        e.ConsentDecision,
        e.AttachmentId is not null,
        e.AmendsId,
        amendsFolio,
        e.Hash ?? "",
        canAmend,
        e.SignatureId is not null);

    private async Task<List<ClinicalEntry>> SealedEntriesAsync(Guid clientId, CancellationToken ct) =>
        await clinical.Entries.Where(e => e.ClientId == clientId && e.Hash != null).OrderBy(e => e.Folio).ToListAsync(ct);

    private async Task EnsureCanSeeAsync(ClinicalEntry entry, CancellationToken ct)
    {
        var clientId = await ResolvePatientAsync(IsDoctor ? entry.ClientId : null, ct);
        if (clientId != entry.ClientId)
            throw new NotFoundException("Adjunto no encontrado.");
        if (IsDoctor && entry.BackgroundKind is null && (entry.SpecialtyId ?? Guid.Empty) != (await OwnSpecialtyAsync(ct)).Id)
            throw new BusinessRuleException("Solo podés ver la historia de tu especialidad.");
    }

    private async Task LogAsync(Guid clientId, ClinicalAccessAction action, string? detail, CancellationToken ct)
    {
        await clinical.RecordAccessAsync(new ClinicalAccess
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            UserId = currentUser.Id,
            UserName = await CurrentUserNameAsync(ct),
            Role = Enum.TryParse<TenantRole>(currentUser.Role, out var role) ? role : null,
            Action = action,
            Detail = detail is { Length: > 200 } ? detail[..200] : detail,
            At = clock.GetUtcNow()
        }, ct);
    }

    private async Task<string> CurrentUserNameAsync(CancellationToken ct)
    {
        var id = currentUser.Id ?? Guid.Empty;
        return await users.Users.Where(u => u.Id == id).Select(u => (u.FirstName + " " + u.LastName).Trim()).FirstOrDefaultAsync(ct) ?? "";
    }

    private async Task EnsurePatientExistsAsync(Guid patientId, CancellationToken ct)
    {
        if (!await clients.Clients.AnyAsync(c => c.Id == patientId, ct))
            throw new NotFoundException("Paciente no encontrado.");
    }

    private async Task<Guid> ResolvePatientAsync(Guid? patientId, CancellationToken ct)
    {
        if (currentUser.Role == TenantRole.Patient.ToString())
        {
            var own = await clients.Clients
                .Where(c => c.UserId == (currentUser.Id ?? Guid.Empty))
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);
            return own == Guid.Empty ? throw new NotFoundException("No hay un paciente asociado a esta cuenta.") : own;
        }
        if (!IsDoctor)
            throw new BusinessRuleException("Solo el profesional puede ver la historia clínica.");
        if (patientId is not Guid clientId)
            throw new BusinessRuleException("Indicá el paciente.");
        await EnsurePatientExistsAsync(clientId, ct);
        return clientId;
    }

    private async Task<HistorySpecialtyDto> OwnSpecialtyAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? Guid.Empty;
        var own = await employees.Employees
            .Where(e => e.UserId == userId && e.SpecialtyId != null)
            .Select(e => new { Id = e.SpecialtyId!.Value, e.Specialty!.Name })
            .FirstOrDefaultAsync(ct);
        return own is null ? new HistorySpecialtyDto(Guid.Empty, NoSpecialty, 0) : new HistorySpecialtyDto(own.Id, own.Name, 0);
    }

    private async Task<string> SpecialtyNameAsync(Guid? specialtyId, CancellationToken ct) =>
        specialtyId is Guid id
            ? await organization.Specialties.Where(s => s.Id == id).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? NoSpecialty
            : NoSpecialty;

    private async Task<Guid?> SpecialtyForAsync(Appointment appointment, CancellationToken ct) =>
        await organization.AppointmentTypes
            .Where(t => t.Id == appointment.AppointmentTypeId)
            .Select(t => t.SpecialtyId)
            .FirstOrDefaultAsync(ct)
        ?? await employees.Employees
            .Where(e => e.UserId == appointment.ProfessionalId)
            .Select(e => e.SpecialtyId)
            .FirstOrDefaultAsync(ct);

    // The first bytes of the file must match the type it claims, so a renamed file is not stored as evidence.
    private static bool MatchesType(byte[] content, string contentType) => contentType switch
    {
        "application/pdf" => content.AsSpan().StartsWith("%PDF"u8),
        "image/jpeg" => content.Length > 2 && content[0] == 0xFF && content[1] == 0xD8,
        "image/png" => content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
        _ => false
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Task<EncounterDto> LoadEncounterAsync(Guid id, CancellationToken ct) =>
        clinical.Encounters.Where(e => e.Id == id).ToEncounterDtos().FirstAsync(ct);
}
