namespace ClinicaSaaS.Domain;

public class EncounterDiagnosis
{
    public Guid EncounterId { get; set; }
    public Guid DiagnosisId { get; set; }
    public Encounter? Encounter { get; set; }
    public Diagnosis? Diagnosis { get; set; }
}
