namespace ClinicaSaaS.Domain;

public class Specialty
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";


    public virtual ICollection<Employee> Employees { get; set; } = [];
    public virtual ICollection<AppointmentType> AppointmentTypes { get; set; } = [];
    public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = [];
}
