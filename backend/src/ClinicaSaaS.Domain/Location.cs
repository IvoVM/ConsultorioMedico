namespace ClinicaSaaS.Domain;

public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
