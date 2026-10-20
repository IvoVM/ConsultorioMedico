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

        var document = DocumentRules.Normalize(command.DocumentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        var email = AccountEmail.ForPatient(document);
        if (await clients.Clients.AnyAsync(c => c.DocumentNumber.ToLower() == document.ToLower(), ct))
            throw new ConflictException("Ya hay un paciente con ese documento.");
        if (await users.FindByEmailAsync(email, ct) is not null)
            throw new ConflictException("Ya hay una cuenta con ese documento.");

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
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
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

    public async Task<PatientLookupDto> FindByDocumentAsync(string? number, CancellationToken ct)
    {
        var document = DocumentRules.Normalize(number);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        var card = await clients.Clients
            .Where(client => client.DocumentNumber == document || client.DocumentNumber.ToLower() == document.ToLower())
            .ToClientCards()
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("No hay un paciente con ese documento.");
        return new PatientLookupDto(
            card.ClientId,
            card.FirstName,
            card.LastName,
            card.DocumentNumber,
            card.BirthDate,
            card.Phone,
            card.HealthInsurance,
            card.MemberNumber,
            card.EmergencyContact,
            card.EmergencyPhone);
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
        await clients.Clients.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToMedicalRecordDtos().ToListAsync(ct);

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
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.DocumentNumber.ToLower().Contains(term) ||
                c.Phone.ToLower().Contains(term) ||
                (c.Record != null && c.Record.HealthInsurance != null && c.Record.HealthInsurance.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.MemberNumber != null && c.Record.MemberNumber.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.EmergencyContact != null && c.Record.EmergencyContact.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.EmergencyPhone != null && c.Record.EmergencyPhone.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.BloodType != null && c.Record.BloodType.ToLower().Contains(term)) ||
                (c.Record != null && c.Record.Allergies != null && c.Record.Allergies.ToLower().Contains(term)));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("phone", false) => source.OrderBy(c => c.Phone).ThenBy(c => c.LastName),
            ("phone", true) => source.OrderByDescending(c => c.Phone).ThenBy(c => c.LastName),
            ("coverage", false) => source.OrderBy(c => c.Record != null ? c.Record.HealthInsurance ?? "Particular" : "Particular").ThenBy(c => c.LastName),
            ("coverage", true) => source.OrderByDescending(c => c.Record != null ? c.Record.HealthInsurance ?? "Particular" : "Particular").ThenBy(c => c.LastName),
            ("emergency", false) => source.OrderBy(c => c.Record != null ? c.Record.EmergencyContact ?? "" : "").ThenBy(c => c.LastName),
            ("emergency", true) => source.OrderByDescending(c => c.Record != null ? c.Record.EmergencyContact ?? "" : "").ThenBy(c => c.LastName),
            ("blood", false) => source.OrderBy(c => c.Record != null ? c.Record.BloodType ?? "" : "").ThenBy(c => c.LastName),
            ("blood", true) => source.OrderByDescending(c => c.Record != null ? c.Record.BloodType ?? "" : "").ThenBy(c => c.LastName),
            ("allergies", false) => source.OrderBy(c => c.Record != null ? c.Record.Allergies ?? "" : "").ThenBy(c => c.LastName),
            ("allergies", true) => source.OrderByDescending(c => c.Record != null ? c.Record.Allergies ?? "" : "").ThenBy(c => c.LastName),
            ("updated", false) => source.OrderBy(c => c.Record != null ? c.Record.UpdatedAt : DateTimeOffset.MinValue).ThenBy(c => c.LastName),
            ("updated", true) => source.OrderByDescending(c => c.Record != null ? c.Record.UpdatedAt : DateTimeOffset.MinValue).ThenBy(c => c.LastName),
            ("patient", true) or ("name", true) => source.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName),
            _ => source.OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
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

        var document = DocumentRules.Normalize(command.DocumentNumber);
        if (!DocumentRules.IsValid(document))
            throw new BusinessRuleException("El documento no es válido.");
        if (await clients.Clients.AnyAsync(c => c.Id != patientId && c.DocumentNumber.ToLower() == document.ToLower(), ct))
            throw new ConflictException("Ya hay un paciente con ese documento.");

        var client = await clients.GetAsync(patientId, ct) ?? throw new NotFoundException("Paciente no encontrado.");
        client.FirstName = command.FirstName.Trim();
        client.LastName = command.LastName.Trim();
        client.DocumentNumber = document;
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

        if (client.UserId is Guid userId)
            await users.UpdateNameAsync(userId, command.FirstName.Trim(), command.LastName.Trim(), ct);
        await audit.RecordAsync(currentUser.Id, "historia", "HistoriaMedica", record.Id.ToString(), client.Id.ToString(), ct);
        return await GetAsync(patientId, ct);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
