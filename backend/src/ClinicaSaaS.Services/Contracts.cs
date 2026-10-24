using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services;

public record TenantAccount(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    TenantRole Role,
    bool MustChangePassword)
{
    public string Name => $"{FirstName} {LastName}".Trim();
}

public record ClinicDto(string Slug, string Name);

public record LoginCommand(string DocumentNumber, string Password);
public record LoginStaffCommand(string DocumentNumber, string Password);
public record ChangePasswordCommand(string Password);
public record PatientProfileDto(string FirstName, string LastName, string DocumentNumber, string Phone);
public record UpdatePatientProfileCommand(string FirstName, string LastName, string Phone);
public record TokenDto(string Token, string Email, string Name, string Role, string? TenantSlug, bool MustChangePassword);

public record IssuedSession(TokenDto Access, string RefreshToken, DateTimeOffset RefreshExpires);

public record RegisterPatientCommand(
    string Password,
    string FirstName,
    string LastName,
    string DocumentNumber,
    string Phone);

public record LocationDto(Guid Id, string Name, string Address, bool IsActive);
public record SaveLocationCommand(string Name, string Address);
public record MedicalServiceDto(Guid Id, Guid LocationId, string Name);
public record SaveMedicalServiceCommand(Guid LocationId, string Name);
public record SpecialtyDto(Guid Id, string Name);
public record SaveSpecialtyCommand(string Name);
public record AppointmentTypeDto(Guid Id, string Name, int DurationMinutes, Guid? SpecialtyId, AppointmentKind Kind);
public record SaveAppointmentTypeCommand(string Name, int DurationMinutes, Guid? SpecialtyId, AppointmentKind Kind);
public record ProfessionalDto(Guid Id, string FirstName, string LastName, string Email, string? LicenseNumber, Guid? SpecialtyId);
public record AssignSpecialtyCommand(Guid? SpecialtyId);

public record EmployeeDto(Guid Id, string FirstName, string LastName, string Email, string? DocumentNumber, TenantRole Role, string? LicenseNumber, string? Specialty, Guid? SpecialtyId, bool Active, IReadOnlyList<Guid> InsurerIds);
public record CreateEmployeeCommand(string FirstName, string LastName, string DocumentNumber, TenantRole Role, string? LicenseNumber, Guid? SpecialtyId, IReadOnlyList<Guid>? InsurerIds = null);
public record UpdateEmployeeCommand(string FirstName, string LastName, string DocumentNumber, TenantRole Role, string? LicenseNumber, Guid? SpecialtyId, IReadOnlyList<Guid>? InsurerIds = null);
public record AccountUserDto(Guid Id, string FirstName, string LastName, string Email, TenantRole Role, string? DocumentNumber);
public record SetEmployeeActiveCommand(bool Active);
public record CreatedEmployeeAccountDto(EmployeeDto Employee, string TemporaryPassword);
public record ImportEmployeesCommand(string Csv);
public record CreatedEmployeeDto(int Row, string DocumentNumber, string TemporaryPassword, TenantRole Role);
public record RejectedRowDto(int Row, string Reason);
public record EmployeeImportDto(IReadOnlyList<CreatedEmployeeDto> Created, IReadOnlyList<RejectedRowDto> Rejected);

public record ScheduleBlockDto(Guid Id, DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime, Guid LocationId, Guid AppointmentTypeId);
public record SaveScheduleCommand(Guid ProfessionalId, IReadOnlyList<ScheduleBlockDto> Blocks);
public record BlockoutDto(Guid Id, Guid ProfessionalId, DateTimeOffset Start, DateTimeOffset End, string Reason);
public record CreateBlockoutCommand(Guid ProfessionalId, DateTimeOffset Start, DateTimeOffset End, string Reason);

public record SlotDto(Guid ProfessionalId, string Professional, Guid LocationId, Guid AppointmentTypeId, string AppointmentType, DateTimeOffset Start, DateTimeOffset End, IReadOnlyList<InsurerDto> Insurers);
public record AvailabilityQuery(Guid LocationId, Guid SpecialtyId, Guid? ProfessionalId, DateOnly Date);

public record BookAppointmentCommand(Guid? PatientId, Guid ProfessionalId, Guid LocationId, Guid AppointmentTypeId, DateTimeOffset Start, string? VisitReason, Guid? InsurerId = null, string? MemberNumber = null);
public record DeskAppointmentCommand(string DocumentNumber, string FirstName, string LastName, string Phone, Guid ProfessionalId, Guid LocationId, Guid AppointmentTypeId, DateTimeOffset Start, Guid? InsurerId, string? MemberNumber);
public record CreateBookingRequestCommand(Guid SpecialtyId, Guid ProfessionalId, Guid LocationId, Guid AppointmentTypeId, DateTimeOffset Start, string Name, string Phone, string DocumentNumber, Guid? InsurerId, string? MemberNumber);
public record BookingRequestDto(Guid Id, string Name, string Phone, string DocumentNumber, string Specialty, string Professional, string AppointmentType, DateTimeOffset Start, string? Insurer, string? MemberNumber, BookingRequestStatus Status, string? RejectionReason, DateTimeOffset? PreviousStart);
public record RejectBookingRequestCommand(string Reason);
public record RescheduleBookingRequestCommand(DateTimeOffset Start);
public record LoginPatientCommand(string DocumentNumber, string Password);
public record ReviewCoverageCommand(bool Accepted);
public record InsurerDto(Guid Id, string Name, bool Enabled);
public record SaveInsurerCommand(string Name);
public record SetInsurerEnabledCommand(bool Enabled);
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
    string? VisitReason,
    string? Insurer,
    string? MemberNumber,
    bool? CoverageAccepted);
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
    string? Notes,
    string? Professional,
    string Specialty);
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
public record PatientSummaryDto(Guid Id, string Name, string DocumentNumber, DateOnly BirthDate, string Phone, string? HealthInsurance, string? MemberNumber);

public record CreatePatientCommand(
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

public record FeeDto(Guid Id, Guid AppointmentTypeId, string AppointmentType, decimal Amount, DateOnly EffectiveFrom, string? Payer);
public record CreateFeeCommand(Guid AppointmentTypeId, decimal Amount, DateOnly EffectiveFrom, string? Payer = null);
public record InsurerPriceDto(string Payer, decimal Amount, DateOnly EffectiveFrom);
public record PrestacionDto(Guid Id, string Name, int DurationMinutes, decimal? Particular, DateOnly? ParticularFrom, IReadOnlyList<InsurerPriceDto> Insurers, AppointmentKind Kind);
public record PrestacionesDto(IReadOnlyList<PrestacionDto> Items, IReadOnlyList<string> Insurers);
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

public record PageDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public record SpecialtyRowDto(Guid Id, string Name, int Professionals);
public record AppointmentTypeRowDto(Guid Id, string Name, int DurationMinutes, string Specialty, int Professionals);
public record FeeRowDto(Guid AppointmentTypeId, string Service, decimal? Amount, DateOnly? EffectiveFrom);
public record MedicalRecordPageDto(IReadOnlyList<MedicalRecordDto> Items, int Total, int Page, int PageSize, int All, int Allergies, int Incomplete);
