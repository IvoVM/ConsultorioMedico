namespace ClinicaSaaS.Domain;

public class EncounterDiagnosis
{
    public Guid EncounterId { get; set; }
    public Guid DiagnosisId { get; set; }
    public virtual Encounter? Encounter { get; set; }
    public virtual Diagnosis? Diagnosis { get; set; }
}
