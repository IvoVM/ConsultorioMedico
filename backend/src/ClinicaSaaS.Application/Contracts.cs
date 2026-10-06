using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application;

public record TenantAccount(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    TenantRole Role,
    string? LicenseNumber,
    Guid? SpecialtyId,
    bool MustChangePassword);

public record ClinicDto(string Slug, string Name);

public record LoginCommand(string Email, string Password);
public record TokenDto(string Token, string Email, string Name, string Role, string? TenantSlug, bool MustChangePassword);

public record IssuedSession(TokenDto Access, string RefreshToken, DateTimeOffset RefreshExpires);

public record RegisterPatientCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string DocumentNumber,
    DateOnly BirthDate,
    string Phone);

public record LocationDto(Guid Id, string Name, string Address, bool IsActive);
public record SaveLocationCommand(string Name, string Address);
public record MedicalServiceDto(Guid Id, Guid LocationId, string Name);
public record SaveMedicalServiceCommand(Guid LocationId, string Name);
public record SpecialtyDto(Guid Id, string Name);
public record SaveSpecialtyCommand(string Name);
public record AppointmentTypeDto(Guid Id, string Name, int DurationMinutes, Guid? SpecialtyId);
public record SaveAppointmentTypeCommand(string Name, int DurationMinutes, Guid? SpecialtyId);
public record ProfessionalDto(Guid Id, string FirstName, string LastName, string Email, string? LicenseNumber, Guid? SpecialtyId);

public record ImportEmployeesCommand(string Csv);
public record CreatedEmployeeDto(int Row, string Email, string TemporaryPassword, TenantRole Role);
public record RejectedRowDto(int Row, string Reason);
public record EmployeeImportDto(IReadOnlyList<CreatedEmployeeDto> Created, IReadOnlyList<RejectedRowDto> Rejected);

public record ScheduleBlockDto(Guid Id, DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime, Guid LocationId, Guid AppointmentTypeId);
public record SaveScheduleCommand(Guid ProfessionalId, IReadOnlyList<ScheduleBlockDto> Blocks);
public record BlockoutDto(Guid Id, Guid ProfessionalId, DateTimeOffset Start, DateTimeOffset End, string Reason);
public record CreateBlockoutCommand(Guid ProfessionalId, DateTimeOffset Start, DateTimeOffset End, string Reason);

public record SlotDto(Guid ProfessionalId, string Professional, Guid LocationId, Guid AppointmentTypeId, string AppointmentType, DateTimeOffset Start, DateTimeOffset End);
public record AvailabilityQuery(Guid LocationId, Guid SpecialtyId, Guid? ProfessionalId, DateOnly Date);

public record BookAppointmentCommand(Guid? PatientId, Guid ProfessionalId, Guid LocationId, Guid AppointmentTypeId, DateTimeOffset Start, string? VisitReason);
public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string Patient,
    Guid ProfessionalId,
    string Professional,
    Guid LocationId,
    string Location,
    Guid AppointmentTypeId,
    string AppointmentType,
    DateTimeOffset Start,
    DateTimeOffset End,
    AppointmentStatus Status,
    string? VisitReason);
public record RescheduleAppointmentCommand(DateTimeOffset Start);

public record WaitlistEntryDto(
    Guid Id,
    Guid PatientId,
    string Patient,
    Guid? ProfessionalId,
    Guid LocationId,
    Guid SpecialtyId,
    WaitlistStatus Status,
    DateTimeOffset CreatedAt,
    string? Notes);
public record CreateWaitlistEntryCommand(Guid? PatientId, Guid? ProfessionalId, Guid LocationId, Guid SpecialtyId, string? Notes);
public record AssignWaitlistEntryCommand(DateTimeOffset Start, Guid AppointmentTypeId);

public record DiagnosisDto(Guid Id, string Code, string Name);
public record SaveEncounterCommand(
    Guid AppointmentId,
    string? Note,
    string? BloodPressure,
    int? HeartRate,
    decimal? Temperature,
    decimal? WeightKg,
    IReadOnlyList<Guid> DiagnosisIds);
public record EncounterDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    Guid ProfessionalId,
    string? Note,
    string? BloodPressure,
    int? HeartRate,
    decimal? Temperature,
    decimal? WeightKg,
    bool IsClosed,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DiagnosisDto> Diagnoses);

public record PrescriptionItemDto(string Medication, string Dose, string Frequency, string Duration);
public record CreatePrescriptionCommand(Guid EncounterId, string? Instructions, IReadOnlyList<PrescriptionItemDto> Items);
public record PrescriptionDto(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid ProfessionalId,
    string Professional,
    string? Instructions,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PrescriptionItemDto> Items);

public record ClinicalHistoryDto(PatientSummaryDto Patient, IReadOnlyList<EncounterDto> Encounters, IReadOnlyList<PrescriptionDto> Prescriptions);
public record PatientSummaryDto(Guid Id, string Name, string DocumentNumber, DateOnly BirthDate, string Phone);

public record CreatePatientCommand(
    string Email,
    string FirstName,
    string LastName,
    string DocumentNumber,
    DateOnly BirthDate,
    string Phone,
    string? HealthInsurance,
    string? MemberNumber,
    string? EmergencyContact,
    string? EmergencyPhone);
public record CreatedPatientDto(MedicalRecordDto Patient, string TemporaryPassword);
public record PatientLookupDto(
    Guid PatientId,
    string FirstName,
    string LastName,
    string DocumentNumber,
    DateOnly BirthDate,
    string Phone,
    string? HealthInsurance,
    string? MemberNumber,
    string? EmergencyContact,
    string? EmergencyPhone);

public record MedicalRecordDto(
    Guid PatientId,
    string FirstName,
    string LastName,
    string Email,
    string DocumentNumber,
    DateOnly BirthDate,
    string Phone,
    string? BloodType,
    string? Allergies,
    string? PersonalHistory,
    string? FamilyHistory,
    string? CurrentMedication,
    string? Habits,
    string? HealthInsurance,
    string? MemberNumber,
    string? EmergencyContact,
    string? EmergencyPhone,
    string? Notes,
    DateTimeOffset? UpdatedAt);
public record SaveMedicalRecordCommand(
    string FirstName,
    string LastName,
    string DocumentNumber,
    DateOnly BirthDate,
    string Phone,
    string? BloodType,
    string? Allergies,
    string? PersonalHistory,
    string? FamilyHistory,
    string? CurrentMedication,
    string? Habits,
    string? HealthInsurance,
    string? MemberNumber,
    string? EmergencyContact,
    string? EmergencyPhone,
    string? Notes);

public record FeeDto(Guid Id, Guid AppointmentTypeId, string AppointmentType, decimal Amount, DateOnly EffectiveFrom);
public record CreateFeeCommand(Guid AppointmentTypeId, decimal Amount, DateOnly EffectiveFrom);
public record InvoiceItemDto(string Description, decimal Amount);
public record InvoiceDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    string Patient,
    decimal Total,
    InvoiceStatus Status,
    PaymentMethod? PaymentMethod,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    IReadOnlyList<InvoiceItemDto> Items);
public record PayInvoiceCommand(PaymentMethod Method);

public record AuditEntryDto(Guid Id, Guid? UserId, string Action, string Entity, string? EntityId, string? Detail, DateTimeOffset Timestamp);
