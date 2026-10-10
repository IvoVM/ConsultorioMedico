namespace ClinicaSaaS.Domain;

public class AppointmentType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; }
    public Guid? SpecialtyId { get; set; }


    public virtual Specialty? Specialty { get; set; }
    public virtual ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = [];
    public virtual ICollection<Appointment> Appointments { get; set; } = [];
    public virtual ICollection<Fee> Fees { get; set; } = [];
}
