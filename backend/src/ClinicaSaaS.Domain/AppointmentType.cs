namespace ClinicaSaaS.Domain;

public class AppointmentType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; }
    public AppointmentKind Kind { get; set; }
    public Guid? SpecialtyId { get; set; }
    // The practice's code in the nomenclator the insurers bill by.
    public string? Code { get; set; }
    // The equipment every appointment of this practice needs, if any.
    public Guid? EquipmentId { get; set; }


    public virtual Specialty? Specialty { get; set; }
    public virtual Equipment? Equipment { get; set; }
    public virtual ICollection<AppointmentTypeSupply> Supplies { get; set; } = [];
    public virtual ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = [];
    public virtual ICollection<Appointment> Appointments { get; set; } = [];
    public virtual ICollection<Fee> Fees { get; set; } = [];
}
