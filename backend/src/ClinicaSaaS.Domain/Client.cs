using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class Client
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string Phone { get; set; } = "";


    public virtual TenantUser User { get; set; } = null!;
    public virtual MedicalRecord? Record { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = [];
    public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = [];
    public virtual ICollection<Encounter> Encounters { get; set; } = [];
    public virtual ICollection<Prescription> Prescriptions { get; set; } = [];
    public virtual ICollection<Invoice> Invoices { get; set; } = [];
}
