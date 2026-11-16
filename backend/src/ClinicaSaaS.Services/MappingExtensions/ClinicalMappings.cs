using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class ClinicalMappings
{
    public static IQueryable<DiagnosisDto> ToDiagnosisDtos(this IQueryable<Diagnosis> query) =>
        query.Select(d => new DiagnosisDto(d.Id, d.Code, d.Name));

    public static IQueryable<PatientSummaryDto> ToPatientSummaries(this IQueryable<Client> query) =>
        query.Select(c => new PatientSummaryDto(
            c.Id,
            c.HistoryNumber,
            (c.FirstName + " " + c.LastName).Trim(),
            c.DocumentNumber,
            c.BirthDate,
            c.Phone,
            c.Record != null ? c.Record.HealthInsurance : null,
            c.Record != null ? c.Record.MemberNumber : null));

    public static IQueryable<EncounterDto> ToEncounterDtos(this IQueryable<Encounter> query) =>
        query.Select(e => new EncounterDto(
            e.Id,
            e.AppointmentId,
            e.Appointment.Start,
            e.ClientId,
            (e.Client.FirstName + " " + e.Client.LastName).Trim(),
            e.ProfessionalId,
            e.SpecialtyId,
            e.Note,
            e.Plan,
            e.BloodPressure,
            e.HeartRate,
            e.Temperature,
            e.WeightKg,
            e.IsClosed,
            e.CreatedAt,
            e.Diagnoses.Select(d => new DiagnosisDto(d.Diagnosis!.Id, d.Diagnosis.Code, d.Diagnosis.Name)).ToList()));
}
