using System.Globalization;
using System.Text;
using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

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
            Phone = PhoneRules.Require(command.Phone)
        }, ct);

        var record = new MedicalRecord
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            HealthInsurance = Clean(command.HealthInsurance),
            MemberNumber = Clean(command.MemberNumber),
            EmergencyContact = Clean(command.EmergencyContact),
            EmergencyPhone = PhoneRules.Optional(command.EmergencyPhone),
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

    public async Task<object> ListResultAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await ListAsync(ct);
        var source = clients.Clients.AsQueryable();
        if (query.Filter == "allergies")
            source = source.Where(c => c.Record != null && c.Record.Allergies != null && c.Record.Allergies != "");
        else if (query.Filter == "incomplete")
            source = source.Where(c => c.Record == null);
        if (query.Term is string term)
            source = source.Where(c =>
                c.User.FirstName.ToLower().Contains(term) ||
                c.User.LastName.ToLower().Contains(term) ||
                c.DocumentNumber.ToLower().Contains(term) ||
                (c.User.Email ?? "").ToLower().Contains(term) ||
                c.Phone.ToLower().Contains(term) ||
                (c.Record != null && c.Record.HealthInsurance != null && c.Record.HealthInsurance.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.MemberNumber != null && c.Record.MemberNumber.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.EmergencyContact != null && c.Record.EmergencyContact.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.EmergencyPhone != null && c.Record.EmergencyPhone.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.BloodType != null && c.Record.BloodType.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.Allergies != null && c.Record.Allergies.ToLower().Contains(term)));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("phone", false) => source.OrderBy(c => c.Phone).ThenBy(c => c.User.LastName),
            ("phone", true) => source.OrderByDescending(c => c.Phone).ThenBy(c => c.User.LastName),
            ("coverage", false) => source.OrderBy(c => c.Record != null ? c.Record.HealthInsurance ?? "Particular" : "Particular").ThenBy(c => c.User.LastName),
            ("coverage", true) => source.OrderByDescending(c => c.Record != null ? c.Record.HealthInsurance ?? "Particular" : "Particular").ThenBy(c => c.User.LastName),
            ("emergency", false) => source.OrderBy(c => c.Record != null ? c.Record.EmergencyContact ?? "" : "").ThenBy(c => c.User.LastName),
            ("emergency", true) => source.OrderByDescending(c => c.Record != null ? c.Record.EmergencyContact ?? "" : "").ThenBy(c => c.User.LastName),
            ("blood", false) => source.OrderBy(c => c.Record != null ? c.Record.BloodType ?? "" : "").ThenBy(c => c.User.LastName),
            ("blood", true) => source.OrderByDescending(c => c.Record != null ? c.Record.BloodType ?? "" : "").ThenBy(c => c.User.LastName),
            ("allergies", false) => source.OrderBy(c => c.Record != null ? c.Record.Allergies ?? "" : "").ThenBy(c => c.User.LastName),
            ("allergies", true) => source.OrderByDescending(c => c.Record != null ? c.Record.Allergies ?? "" : "").ThenBy(c => c.User.LastName),
            ("updated", false) => source.OrderBy(c => c.Record != null ? c.Record.UpdatedAt : DateTimeOffset.MinValue).ThenBy(c => c.User.LastName),
            ("updated", true) => source.OrderByDescending(c => c.Record != null ? c.Record.UpdatedAt : DateTimeOffset.MinValue).ThenBy(c => c.User.LastName),
            ("patient", true) or ("name", true) => source.OrderByDescending(c => c.User.LastName).ThenByDescending(c => c.User.FirstName),
            _ => source.OrderBy(c => c.User.LastName).ThenBy(c => c.User.FirstName)
        };
        var page = await Pages.Materialize(ordered.ToMedicalRecordDtos(), query, ct);
        var all = await clients.Clients.CountAsync(ct);
        var allergies = await clients.Clients.CountAsync(c => c.Record != null && c.Record.Allergies != null && c.Record.Allergies != "", ct);
        var incomplete = await clients.Clients.CountAsync(c => c.Record == null, ct);
        return new MedicalRecordPageDto(page.Items, page.Total, page.Page, page.PageSize, all, allergies, incomplete);
    }

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
        client.Phone = PhoneRules.Require(command.Phone);

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
        record.EmergencyPhone = PhoneRules.Optional(command.EmergencyPhone);
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
