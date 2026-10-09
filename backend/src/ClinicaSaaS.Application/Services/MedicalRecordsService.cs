using System.Globalization;
using System.Text;
using ClinicaSaaS.Application.Services.MappingExtensions;
using ClinicaSaaS.Application.Services.Utilities;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Application.Services;

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
        if (await clients.Clients.AnyAsync(c => c.DocumentNumber.ToLower() == document.ToLower(), ct))
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
        var cards = await clients.Clients.ToClientCards().ToListAsync(ct);
        return cards
            .Where(card => Matches(card, needle))
            .OrderBy(card => card.LastName)
            .ThenBy(card => card.FirstName)
            .Take(12)
            .Select(card => new PatientLookupDto(
                card.ClientId,
                card.FirstName,
                card.LastName,
                card.DocumentNumber,
                card.BirthDate,
                card.Phone,
                card.HealthInsurance,
                card.MemberNumber,
                card.EmergencyContact,
                card.EmergencyPhone))
            .ToList();
    }

    private static bool Matches(ClientCard card, string needle)
    {
        var haystack = Normalize(string.Join(' ',
            card.FirstName,
            card.LastName,
            card.DocumentNumber,
            card.Phone,
            card.HealthInsurance,
            card.MemberNumber,
            card.EmergencyContact,
            card.EmergencyPhone));
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

    public async Task<IReadOnlyList<MedicalRecordDto>> ListAsync(CancellationToken ct) =>
        await clients.Clients.OrderBy(c => c.User.LastName).ThenBy(c => c.User.FirstName).ToMedicalRecordDtos().ToListAsync(ct);

    public async Task<MedicalRecordDto> GetAsync(Guid patientId, CancellationToken ct) =>
        await clients.Clients.Where(c => c.Id == patientId).ToMedicalRecordDtos().FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Paciente no encontrado.");

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
}
