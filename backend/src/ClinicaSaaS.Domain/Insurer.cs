namespace ClinicaSaaS.Domain;

public class Insurer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
