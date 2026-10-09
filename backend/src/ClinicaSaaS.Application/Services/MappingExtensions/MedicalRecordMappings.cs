using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;

namespace ClinicaSaaS.Application.Services.MappingExtensions;

public static class MedicalRecordMappings
{
    public static IQueryable<ClientCard> ToClientCards(this IQueryable<Client> query) =>
        query.Select(c => new ClientCard
        {
            ClientId = c.Id,
            FirstName = c.User.FirstName,
            LastName = c.User.LastName,
            DocumentNumber = c.DocumentNumber,
            BirthDate = c.BirthDate,
            Phone = c.Phone,
            HealthInsurance = c.Record != null ? c.Record.HealthInsurance : null,
            MemberNumber = c.Record != null ? c.Record.MemberNumber : null,
            EmergencyContact = c.Record != null ? c.Record.EmergencyContact : null,
            EmergencyPhone = c.Record != null ? c.Record.EmergencyPhone : null
        });

    public static IQueryable<MedicalRecordDto> ToMedicalRecordDtos(this IQueryable<Client> query) =>
        query.Select(c => new MedicalRecordDto(
            c.Id,
            c.User.FirstName,
            c.User.LastName,
            c.User.Email ?? "",
            c.DocumentNumber,
            c.BirthDate,
            c.Phone,
            c.Record != null ? c.Record.BloodType : null,
            c.Record != null ? c.Record.Allergies : null,
            c.Record != null ? c.Record.PersonalHistory : null,
            c.Record != null ? c.Record.FamilyHistory : null,
            c.Record != null ? c.Record.CurrentMedication : null,
            c.Record != null ? c.Record.Habits : null,
            c.Record != null ? c.Record.HealthInsurance : null,
            c.Record != null ? c.Record.MemberNumber : null,
            c.Record != null ? c.Record.EmergencyContact : null,
            c.Record != null ? c.Record.EmergencyPhone : null,
            c.Record != null ? c.Record.Notes : null,
            c.Record != null ? (DateTimeOffset?)c.Record.UpdatedAt : null));
}
