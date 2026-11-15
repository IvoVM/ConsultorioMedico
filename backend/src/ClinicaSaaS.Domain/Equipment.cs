namespace ClinicaSaaS.Domain;

// A piece of equipment a practice needs (an ultrasound, a dental chair). Only one appointment uses it at a time.
public class Equipment
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;


    public virtual Location Location { get; set; } = null!;
}
