namespace ClinicaSaaS.Domain;

// A consulting room inside a site. A weekly block says which room the professional works in, and each
// appointment takes that room, so two appointments never share it at the same time.
public class Room
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;


    public virtual Location Location { get; set; } = null!;
}
