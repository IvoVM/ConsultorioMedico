using System.Globalization;
using System.Text;
using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class MedicalRecordsService(
    IMedicalRecordStore records,
    IPatientStore patients,
    ITenantUserStore users,
    ICurrentUser currentUser,
    IAuditStore audit,
    IValidator<SaveMedicalRecordCommand> validator,
    IValidator<CreatePatientCommand> createValidator,
    TimeProvider clock)
{
    public async Task<CreatedPatientDto> CreateAsync(CreatePatientCommand command, CancellationToken ct)
    {
        var result = await createValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var email = command.Email.Trim();
        var document = command.DocumentNumber.Trim();
        if (await users.FindByEmailAsync(email, ct) is not null)
            throw new ConflictException("Ese email ya está registrado.");
        if ((await records.PatientsAsync(ct)).Any(p => string.Equals(p.DocumentNumber, document, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException("Ya hay un paciente con ese documento.");

        var password = TemporaryPassword.Generate();
        var account = await users.CreateAsync(
            email,
            password,
            command.FirstName.Trim(),
            command.LastName.Trim(),
            TenantRole.Patient,
            null,
            null,
            true,
            ct);
        var patient = await patients.AddAsync(new Patient
        {
            Id = Guid.NewGuid(),
            UserId = account.Id,
            DocumentNumber = document,
            BirthDate = command.BirthDate,
            Phone = command.Phone.Trim()
        }, ct);

        var record = new MedicalRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            HealthInsurance = Clean(command.HealthInsurance),
            MemberNumber = Clean(command.MemberNumber),
            EmergencyContact = Clean(command.EmergencyContact),
            EmergencyPhone = Clean(command.EmergencyPhone),
            UpdatedAt = clock.GetUtcNow(),
            UpdatedBy = currentUser.Id
        };
        records.Add(record);
        await records.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "alta", "Paciente", patient.Id.ToString(), email, ct);
        return new CreatedPatientDto(await GetAsync(patient.Id, ct), password);
    }

    public async Task<IReadOnlyList<PatientLookupDto>> SearchAsync(string? query, CancellationToken ct)
    {
        var text = query?.Trim() ?? "";
        if (text.Length < 2)
            throw new BusinessRuleException("Escribí al menos 2 caracteres: documento, nombre o afiliado.");

        var needle = Normalize(text);
        var patientList = await records.PatientsAsync(ct);
        var accounts = (await users.ListByRoleAsync(TenantRole.Patient, null, ct)).ToDictionary(u => u.Id);
        var recordsByPatient = (await records.ListAsync(ct)).ToDictionary(r => r.PatientId);

        return patientList
            .Select(patient => (
                patient,
                account: accounts.GetValueOrDefault(patient.UserId),
                record: recordsByPatient.GetValueOrDefault(patient.Id)))
            .Where(item => Matches(item.patient, item.account, item.record, needle))
            .OrderBy(item => item.account?.LastName)
            .ThenBy(item => item.account?.FirstName)
            .Take(12)
            .Select(item => new PatientLookupDto(
                item.patient.Id,
                item.account?.FirstName ?? "",
                item.account?.LastName ?? "",
                item.patient.DocumentNumber,
                item.patient.BirthDate,
                item.patient.Phone,
                item.record?.HealthInsurance,
                item.record?.MemberNumber,
                item.record?.EmergencyContact,
                item.record?.EmergencyPhone))
            .ToList();
    }

    private static bool Matches(Patient patient, TenantAccount? account, MedicalRecord? record, string needle)
    {
        var haystack = Normalize(string.Join(' ',
            account?.FirstName,
            account?.LastName,
            patient.DocumentNumber,
            patient.Phone,
            record?.HealthInsurance,
            record?.MemberNumber,
            record?.EmergencyContact,
            record?.EmergencyPhone));
        return haystack.Contains(needle, StringComparison.Ordinal);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var chars = decomposed.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c));
        return new string(chars.ToArray()).ToLowerInvariant();
    }

    public async Task<IReadOnlyList<MedicalRecordDto>> ListAsync(CancellationToken ct)
    {
        var patientList = await records.PatientsAsync(ct);
        var accounts = (await users.ListByRoleAsync(TenantRole.Patient, null, ct)).ToDictionary(u => u.Id);
        var recordsByPatient = (await records.ListAsync(ct)).ToDictionary(r => r.PatientId);
        return patientList
            .Select(p => Map(p, accounts.GetValueOrDefault(p.UserId), recordsByPatient.GetValueOrDefault(p.Id)))
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToList();
    }

    public async Task<MedicalRecordDto> GetAsync(Guid patientId, CancellationToken ct)
    {
        var patient = await patients.GetAsync(patientId, ct) ?? throw new NotFoundException("Paciente no encontrado.");
        var account = await users.FindByIdAsync(patient.UserId, ct);
        return Map(patient, account, await records.GetByPatientAsync(patientId, ct));
    }

    public async Task<MedicalRecordDto> SaveAsync(Guid patientId, SaveMedicalRecordCommand command, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var patient = await patients.GetAsync(patientId, ct) ?? throw new NotFoundException("Paciente no encontrado.");
        patient.DocumentNumber = command.DocumentNumber.Trim();
        patient.BirthDate = command.BirthDate;
        patient.Phone = command.Phone.Trim();

        var record = await records.GetByPatientAsync(patientId, ct);
        if (record is null)
        {
            record = new MedicalRecord { Id = Guid.NewGuid(), PatientId = patientId };
            records.Add(record);
        }
        record.BloodType = Clean(command.BloodType);
        record.Allergies = Clean(command.Allergies);
        record.PersonalHistory = Clean(command.PersonalHistory);
        record.FamilyHistory = Clean(command.FamilyHistory);
        record.CurrentMedication = Clean(command.CurrentMedication);
        record.Habits = Clean(command.Habits);
        record.HealthInsurance = Clean(command.HealthInsurance);
        record.MemberNumber = Clean(command.MemberNumber);
        record.EmergencyContact = Clean(command.EmergencyContact);
        record.EmergencyPhone = Clean(command.EmergencyPhone);
        record.Notes = Clean(command.Notes);
        record.UpdatedAt = clock.GetUtcNow();
        record.UpdatedBy = currentUser.Id;
        await records.SaveAsync(ct);

        await users.UpdateNameAsync(patient.UserId, command.FirstName.Trim(), command.LastName.Trim(), ct);
        await audit.RecordAsync(currentUser.Id, "historia", "HistoriaMedica", record.Id.ToString(), patient.Id.ToString(), ct);
        return await GetAsync(patientId, ct);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static MedicalRecordDto Map(Patient patient, TenantAccount? account, MedicalRecord? record) => new(
        patient.Id,
        account?.FirstName ?? "",
        account?.LastName ?? "",
        account?.Email ?? "",
        patient.DocumentNumber,
        patient.BirthDate,
        patient.Phone,
        record?.BloodType,
        record?.Allergies,
        record?.PersonalHistory,
        record?.FamilyHistory,
        record?.CurrentMedication,
        record?.Habits,
        record?.HealthInsurance,
        record?.MemberNumber,
        record?.EmergencyContact,
        record?.EmergencyPhone,
        record?.Notes,
        record?.UpdatedAt);
}
