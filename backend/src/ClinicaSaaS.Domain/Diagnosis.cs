namespace ClinicaSaaS.Domain;

public class Diagnosis
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";


    public virtual ICollection<EncounterDiagnosis> EncounterDiagnoses { get; set; } = [];
}
