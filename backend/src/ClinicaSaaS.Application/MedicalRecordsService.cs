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
    TimeProvider clock)
{
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
