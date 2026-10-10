namespace ClinicaSaaS.Domain;

public class MedicalService
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = "";


    public virtual Location Location { get; set; } = null!;
}
