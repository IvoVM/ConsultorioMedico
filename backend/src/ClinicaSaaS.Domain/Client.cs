using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

public class Client
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string Phone { get; set; } = "";
    // The single clinical record number the patient is told (Ley 26.529, art. 17). The database hands it out.
    public int HistoryNumber { get; set; }


    public virtual TenantUser? User { get; set; }
    public virtual MedicalRecord? Record { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = [];
    public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = [];
    public virtual ICollection<Encounter> Encounters { get; set; } = [];
    public virtual ICollection<Prescription> Prescriptions { get; set; } = [];
    public virtual ICollection<Invoice> Invoices { get; set; } = [];
    public virtual ICollection<ClinicalEntry> ClinicalEntries { get; set; } = [];
}
