namespace ClinicaSaaS.Domain;

public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsActive { get; set; } = true;


    public virtual ICollection<MedicalService> Services { get; set; } = [];
    public virtual ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = [];
    public virtual ICollection<Appointment> Appointments { get; set; } = [];
    public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = [];
}
