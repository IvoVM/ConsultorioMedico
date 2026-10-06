namespace ClinicaSaaS.Domain;

public class Tenant
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public TenantType Type { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string ProtectedConnectionString { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class MedicalService
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = "";
}

public class Specialty
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class AppointmentType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; }
    public Guid? SpecialtyId { get; set; }
}

public class ScheduleBlock
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public Guid LocationId { get; set; }
    public Guid AppointmentTypeId { get; set; }
}

public class ScheduleBlockout
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string Reason { get; set; } = "";
}

public class Patient
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string Phone { get; set; } = "";
}

public class MedicalRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? PersonalHistory { get; set; }
    public string? FamilyHistory { get; set; }
    public string? CurrentMedication { get; set; }
    public string? Habits { get; set; }
    public string? HealthInsurance { get; set; }
    public string? MemberNumber { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public class Appointment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public string? VisitReason { get; set; }
}

public class WaitlistEntry
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? ProfessionalId { get; set; }
    public Guid LocationId { get; set; }
    public Guid SpecialtyId { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class Diagnosis
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class Encounter
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? Note { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? WeightKg { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<EncounterDiagnosis> Diagnoses { get; set; } = [];
}

public class EncounterDiagnosis
{
    public Guid EncounterId { get; set; }
    public Guid DiagnosisId { get; set; }
    public Encounter? Encounter { get; set; }
    public Diagnosis? Diagnosis { get; set; }
}

public class Prescription
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? Instructions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PrescriptionItem> Items { get; set; } = [];
}

public class PrescriptionItem
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string Medication { get; set; } = "";
    public string Dose { get; set; } = "";
    public string Frequency { get; set; } = "";
    public string Duration { get; set; } = "";
    public Prescription? Prescription { get; set; }
}

public class Fee
{
    public Guid Id { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}

public class Invoice
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public List<InvoiceItem> Items { get; set; } = [];
}

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public Invoice? Invoice { get; set; }
}

public class AuditEntry
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
