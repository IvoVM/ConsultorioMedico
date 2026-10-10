namespace ClinicaSaaS.Domain;

public class PrescriptionItem
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string Medication { get; set; } = "";
    public string Dose { get; set; } = "";
    public string Frequency { get; set; } = "";
    public string Duration { get; set; } = "";


    public virtual Prescription? Prescription { get; set; }
}
