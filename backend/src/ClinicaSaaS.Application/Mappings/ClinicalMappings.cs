using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application.Mappings;

public static class ClinicalMappings
{
    public static IQueryable<DiagnosisDto> ToDiagnosisDtos(this IQueryable<Diagnosis> query) =>
        query.Select(d => new DiagnosisDto(d.Id, d.Code, d.Name));

    public static IQueryable<PatientSummaryDto> ToPatientSummaries(this IQueryable<Client> query) =>
        query.Select(c => new PatientSummaryDto(
            c.Id,
            (c.User.FirstName + " " + c.User.LastName).Trim(),
            c.DocumentNumber,
            c.BirthDate,
            c.Phone));

    public static IQueryable<EncounterDto> ToEncounterDtos(this IQueryable<Encounter> query) =>
        query.Select(e => new EncounterDto(
            e.Id,
            e.AppointmentId,
            e.ClientId,
            e.ProfessionalId,
            e.Note,
            e.BloodPressure,
            e.HeartRate,
            e.Temperature,
            e.WeightKg,
            e.IsClosed,
            e.CreatedAt,
            e.Diagnoses.Select(d => new DiagnosisDto(d.Diagnosis!.Id, d.Diagnosis.Code, d.Diagnosis.Name)).ToList()));

    public static IQueryable<PrescriptionDto> ToPrescriptionDtos(this IQueryable<Prescription> query) =>
        query.Select(p => new PrescriptionDto(
            p.Id,
            p.EncounterId,
            p.ClientId,
            p.ProfessionalId,
            (p.Professional.FirstName + " " + p.Professional.LastName).Trim(),
            p.Instructions,
            p.CreatedAt,
            p.Items.Select(i => new PrescriptionItemDto(i.Medication, i.Dose, i.Frequency, i.Duration)).ToList()));
}
