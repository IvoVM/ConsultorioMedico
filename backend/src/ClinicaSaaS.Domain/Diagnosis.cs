namespace ClinicaSaaS.Domain;

public class Diagnosis
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
