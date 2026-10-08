using System.Globalization;
using System.Text;
using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class MedicalRecordsService(
    IMedicalRecordStore records,
    IClientStore clients,
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
        if ((await records.ClientsAsync(ct)).Any(c => string.Equals(c.DocumentNumber, document, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException("Ya hay un paciente con ese documento.");

        var password = TemporaryPassword.Generate();
        var account = await users.CreateAsync(
            email,
            password,
            command.FirstName.Trim(),
            command.LastName.Trim(),
            TenantRole.Patient,
            true,
            ct);
        var client = await clients.AddAsync(new Client
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
            ClientId = client.Id,
            HealthInsurance = Clean(command.HealthInsurance),
            MemberNumber = Clean(command.MemberNumber),
            EmergencyContact = Clean(command.EmergencyContact),
            EmergencyPhone = Clean(command.EmergencyPhone),
            UpdatedAt = clock.GetUtcNow(),
            UpdatedBy = currentUser.Id
        };
        records.Add(record);
        await records.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "alta", "Paciente", client.Id.ToString(), email, ct);
        return new CreatedPatientDto(await GetAsync(client.Id, ct), password);
    }

    public async Task<IReadOnlyList<PatientLookupDto>> SearchAsync(string? query, CancellationToken ct)
    {
        var text = query?.Trim() ?? "";
        if (text.Length < 2)
            throw new BusinessRuleException("Escribí al menos 2 caracteres: documento, nombre o afiliado.");

        var needle = Normalize(text);
        var clientList = await records.ClientsAsync(ct);
        var accounts = (await users.ListByRoleAsync(TenantRole.Patient, ct)).ToDictionary(u => u.Id);
        var recordsByClient = (await records.ListAsync(ct)).ToDictionary(r => r.ClientId);

        return clientList
            .Select(client => (
                client,
                account: accounts.GetValueOrDefault(client.UserId),
                record: recordsByClient.GetValueOrDefault(client.Id)))
            .Where(item => Matches(item.client, item.account, item.record, needle))
            .OrderBy(item => item.account?.LastName)
            .ThenBy(item => item.account?.FirstName)
            .Take(12)
            .Select(item => new PatientLookupDto(
                item.client.Id,
                item.account?.FirstName ?? "",
                item.account?.LastName ?? "",
                item.client.DocumentNumber,
                item.client.BirthDate,
                item.client.Phone,
                item.record?.HealthInsurance,
                item.record?.MemberNumber,
                item.record?.EmergencyContact,
                item.record?.EmergencyPhone))
            .ToList();
    }

    private static bool Matches(Client client, TenantAccount? account, MedicalRecord? record, string needle)
    {
        var haystack = Normalize(string.Join(' ',
            account?.FirstName,
            account?.LastName,
            client.DocumentNumber,
            client.Phone,
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
        var clientList = await records.ClientsAsync(ct);
        var accounts = (await users.ListByRoleAsync(TenantRole.Patient, ct)).ToDictionary(u => u.Id);
        var recordsByClient = (await records.ListAsync(ct)).ToDictionary(r => r.ClientId);
        return clientList
            .Select(c => Map(c, accounts.GetValueOrDefault(c.UserId), recordsByClient.GetValueOrDefault(c.Id)))
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToList();
    }

    public async Task<MedicalRecordDto> GetAsync(Guid patientId, CancellationToken ct)
    {
        var client = await clients.GetAsync(patientId, ct) ?? throw new NotFoundException("Paciente no encontrado.");
        var account = await users.FindByIdAsync(client.UserId, ct);
        return Map(client, account, await records.GetByClientAsync(patientId, ct));
    }

    public async Task<MedicalRecordDto> SaveAsync(Guid patientId, SaveMedicalRecordCommand command, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var client = await clients.GetAsync(patientId, ct) ?? throw new NotFoundException("Paciente no encontrado.");
        client.DocumentNumber = command.DocumentNumber.Trim();
        client.BirthDate = command.BirthDate;
        client.Phone = command.Phone.Trim();

        var record = await records.GetByClientAsync(patientId, ct);
        if (record is null)
        {
            record = new MedicalRecord { Id = Guid.NewGuid(), ClientId = patientId };
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

        await users.UpdateNameAsync(client.UserId, command.FirstName.Trim(), command.LastName.Trim(), ct);
        await audit.RecordAsync(currentUser.Id, "historia", "HistoriaMedica", record.Id.ToString(), client.Id.ToString(), ct);
        return await GetAsync(patientId, ct);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static MedicalRecordDto Map(Client client, TenantAccount? account, MedicalRecord? record) => new(
        client.Id,
        account?.FirstName ?? "",
        account?.LastName ?? "",
        account?.Email ?? "",
        client.DocumentNumber,
        client.BirthDate,
        client.Phone,
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
