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

public record ClinicDto(string Slug, string Name, bool BillingEnabled, bool FiscalEnabled);
public record ClinicSettingsDto(string Name, bool BillingEnabled, bool AutoInvoice, bool FiscalEnabled, string? TaxId, TaxCondition? TaxCondition, int? PointOfSale, bool AutoAuthorize);
public record SaveClinicSettingsCommand(
    string Name,
    bool BillingEnabled,
    bool AutoInvoice,
    bool FiscalEnabled = false,
    string? TaxId = null,
    TaxCondition? TaxCondition = null,
    int? PointOfSale = null,
    bool AutoAuthorize = false);

public record LoginCommand(string DocumentNumber, string Password, LoginAccount? Account = null);
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
public record BookingRequestDto(Guid Id, string Name, string Phone, string DocumentNumber, string Specialty, string Professional, string AppointmentType, DateTimeOffset Start, string? Insurer, string? MemberNumber, BookingRequestStatus Status, string? RejectionReason, DateTimeOffset? PreviousStart, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string Location);
public record RejectBookingRequestCommand(string Reason);
public record RescheduleBookingRequestCommand(DateTimeOffset Start);
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
    bool? CoverageAccepted,
    bool IsOverbook = false,
    string? Specialty = null,
    DateTimeOffset? CreatedAt = null,
    string? CreatedBy = null,
    DateTimeOffset? UpdatedAt = null,
    string? UpdatedBy = null);
public record RescheduleAppointmentCommand(DateTimeOffset Start);

public sealed class AppointmentHistoryFilter
{
    public Guid? SpecialtyId { get; set; }
    public Guid? ProfessionalId { get; set; }
    public Guid? PatientId { get; set; }
    public AppointmentStatus? Status { get; set; }
    public bool Past { get; set; }
    // A cancelled booking leaves the agenda, so the history lists it apart: true shows only those, false leaves them out.
    public bool Cancelled { get; set; }
}

// One step in an appointment's life; By and ByRole are null when the system made it (seed, import).
public record AppointmentChangeDto(
    Guid Id,
    AppointmentStatus Status,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Professional,
    DateTimeOffset At,
    string? By,
    Guid ProfessionalId,
    TenantRole? ByRole);

// What the admin writes over a step of the trail: any status, slot, professional and moment, whatever really happened.
public record SaveAppointmentChangeCommand(
    AppointmentStatus Status,
    DateTimeOffset Start,
    DateTimeOffset End,
    Guid ProfessionalId,
    DateTimeOffset At);

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
    Guid? SpecialtyId,
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

public record ClinicalHistoryDto(
    PatientSummaryDto Patient,
    HistorySpecialtyDto? Specialty,
    IReadOnlyList<HistorySpecialtyDto> Specialties,
    IReadOnlyList<EncounterDto> Encounters,
    IReadOnlyList<PrescriptionDto> Prescriptions);
public record HistorySpecialtyDto(Guid Id, string Name, int Encounters);
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

public record FeeDto(Guid Id, Guid AppointmentTypeId, string AppointmentType, decimal Amount, DateOnly EffectiveFrom, string? Payer, decimal? Copay);
public record CreateFeeCommand(Guid AppointmentTypeId, decimal Amount, DateOnly EffectiveFrom, string? Payer = null, decimal? Copay = null);
public record InsurerPriceDto(string Payer, decimal Amount, DateOnly EffectiveFrom, decimal? Copay);
public record PrestacionDto(Guid Id, string Name, int DurationMinutes, decimal? Particular, DateOnly? ParticularFrom, IReadOnlyList<InsurerPriceDto> Insurers, AppointmentKind Kind);
public record PrestacionesDto(IReadOnlyList<PrestacionDto> Items, IReadOnlyList<string> Insurers);
public record InvoiceItemDto(string Description, int Quantity, decimal UnitPrice, decimal Amount);
public record InvoicePaymentDto(Guid Id, decimal Amount, PaymentMethod Method, DateTimeOffset PaidAt, CardBrand? CardBrand, int? CardInstallments);
public record InstallmentDto(int Number, DateOnly DueDate, decimal Amount, decimal Paid, InstallmentStatus Status);
public record PaymentPlanDto(Guid Id, decimal Amount, DateTimeOffset CreatedAt, IReadOnlyList<InstallmentDto> Installments);
public record InvoiceDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    string Patient,
    string PatientDocument,
    string Professional,
    DateTimeOffset AppointmentStart,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    string? Insurer,
    decimal InsurerAmount,
    decimal PatientAmount,
    decimal Paid,
    decimal Balance,
    InvoiceStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? AuthorizedAt,
    FiscalVoucherType? VoucherType,
    int? PointOfSale,
    int? Number,
    string? Cae,
    DateOnly? CaeDueDate,
    bool FiscalSimulated,
    DateTimeOffset? VoidedAt,
    string? VoidReason,
    FiscalVoucherType? CreditNoteType,
    int? CreditNoteNumber,
    string? CreditNoteCae,
    bool Editable,
    IReadOnlyList<InvoiceItemDto> Items,
    IReadOnlyList<InvoicePaymentDto> Payments,
    PaymentPlanDto? Plan,
    // The patient's credit not yet applied to any invoice, which can pay this one.
    decimal PatientCredit);
// Without an amount the payment covers whatever the patient still owes. A card payment carries the
// brand, installments, batch and coupon the processor settles it by.
public record PayInvoiceCommand(
    PaymentMethod Method,
    decimal? Amount = null,
    CardBrand? CardBrand = null,
    int? CardInstallments = null,
    string? CardBatch = null,
    string? CardCoupon = null);
public record CreatePlanCommand(int Installments, DateOnly FirstDueDate);
// Without an amount it applies as much credit as the invoice still owes.
public record ApplyCreditCommand(decimal? Amount = null);

public record AccountMovementDto(
    DateTimeOffset At,
    AccountMovementKind Kind,
    decimal Debit,
    decimal Credit,
    decimal Balance,
    Guid? InvoiceId,
    FiscalVoucherType? VoucherType,
    int? PointOfSale,
    int? Number,
    PaymentMethod? Method,
    CardBrand? CardBrand,
    string? Note);
public record AccountDto(
    Guid PatientId,
    string Patient,
    string PatientDocument,
    // What the patient owes across their invoices, and the money they paid that no invoice has used yet.
    decimal Owed,
    decimal Credit,
    decimal Overdue,
    IReadOnlyList<AccountMovementDto> Movements,
    IReadOnlyList<InvoiceDto> Invoices);
// Money on account: applied to the listed invoices, or to the oldest ones owing, and the rest kept as credit.
public record AccountPaymentCommand(
    Guid LocationId,
    decimal Amount,
    PaymentMethod Method,
    CardBrand? CardBrand = null,
    int? CardInstallments = null,
    string? CardBatch = null,
    string? CardCoupon = null,
    IReadOnlyList<Guid>? InvoiceIds = null,
    string? Note = null);
public record RefundCommand(Guid LocationId, decimal Amount, PaymentMethod Method, string Reason);

public record CashTotalDto(PaymentMethod Method, decimal In, decimal Out, decimal Net);
public record CashMovementDto(Guid Id, CashMovementKind Kind, PaymentMethod Method, decimal Amount, string Description, DateTimeOffset At, string? User);
public record CashSessionDto(
    Guid Id,
    Guid LocationId,
    string Location,
    CashSessionStatus Status,
    decimal OpeningCash,
    DateTimeOffset OpenedAt,
    string? OpenedBy,
    DateTimeOffset? ClosedAt,
    string? ClosedBy,
    decimal ExpectedCash,
    decimal? CountedCash,
    decimal? Difference,
    string? Notes,
    IReadOnlyList<CashTotalDto> Totals,
    IReadOnlyList<CashMovementDto> Movements);
public record OpenCashCommand(Guid LocationId, decimal OpeningCash);
public record CashMovementCommand(CashMovementKind Kind, PaymentMethod Method, decimal Amount, string Description);
public record CloseCashCommand(decimal CountedCash, string? Notes);

public record SettlementLineDto(Guid Id, string Batch, string Coupon, int Installments, DateOnly ChargedOn, DateOnly PaidOn, decimal Gross, decimal Fee, decimal Net, Guid? ReceiptId, string? Patient);
public record CardSettlementDto(Guid Id, CardBrand Brand, DateOnly From, DateOnly To, DateTimeOffset ImportedAt, bool Simulated, decimal Gross, decimal Fee, decimal Net, IReadOnlyList<SettlementLineDto> Lines);
public record CardReceiptDto(Guid Id, string Patient, CardBrand Brand, int Installments, string Batch, string Coupon, decimal Amount, DateTimeOffset PaidAt, bool Reconciled);
public record ReconciliationDto(IReadOnlyList<CardSettlementDto> Settlements, IReadOnlyList<CardReceiptDto> Pending, bool Simulated);
public record ImportSettlementCommand(CardBrand Brand, DateOnly From, DateOnly To);
public record MatchLineCommand(Guid ReceiptId);
public record IssueInvoiceCommand(Guid AppointmentId);
public record InvoiceLineCommand(string Description, int Quantity, decimal UnitPrice);
public record UpdateInvoiceCommand(IReadOnlyList<InvoiceLineCommand> Items, decimal Discount, decimal InsurerAmount);
public record VoidInvoiceCommand(string Reason);
public record FileDto(byte[] Content, string ContentType, string Name);

public record AuditEntryDto(Guid Id, Guid? UserId, string Action, string Entity, string? EntityId, string? Detail, DateTimeOffset Timestamp);

public record PageDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public record SpecialtyRowDto(Guid Id, string Name, int Professionals);
public record AppointmentTypeRowDto(Guid Id, string Name, int DurationMinutes, string Specialty, int Professionals);
public record FeeRowDto(Guid AppointmentTypeId, string Service, decimal? Amount, DateOnly? EffectiveFrom);
public record MedicalRecordPageDto(IReadOnlyList<MedicalRecordDto> Items, int Total, int Page, int PageSize, int All, int Allergies, int Incomplete);

public record AgendaRangeDto(DateTimeOffset Start, DateTimeOffset End);
public record AgendaTypeDto(Guid Id, string Name, int DurationMinutes);
public record AgendaProfessionalDto(
    Guid Id,
    string Name,
    string? Specialty,
    IReadOnlyList<AgendaRangeDto> Ranges,
    IReadOnlyList<AgendaRangeDto> Blockouts,
    IReadOnlyList<AgendaTypeDto> Types,
    IReadOnlyList<InsurerDto> Insurers);
public record AgendaDayDto(DateOnly Date, bool CanEdit, IReadOnlyList<AgendaProfessionalDto> Professionals, IReadOnlyList<AppointmentDto> Appointments);
public record MoveAppointmentCommand(Guid ProfessionalId, DateTimeOffset Start);
public record AgendaBookCommand(
    string DocumentNumber,
    string? FirstName,
    string? LastName,
    string? Phone,
    Guid ProfessionalId,
    Guid AppointmentTypeId,
    DateTimeOffset Start,
    Guid? InsurerId,
    string? MemberNumber);
public record PlaceRequestCommand(Guid ProfessionalId, DateTimeOffset Start);
public record EditAppointmentCommand(Guid? InsurerId, string? MemberNumber);

// Percent is null when the period has no appointments, so the dashboard shows a dash instead of a misleading 0 %.
public record StatusShareDto(int Count, decimal? Percent);
public record StatusCountDto(AppointmentStatus Status, int Count);
public record AppointmentMetricsDto(
    int Total,
    StatusShareDto Completed,
    StatusShareDto NoShow,
    StatusShareDto Cancelled,
    StatusShareDto Rescheduled,
    IReadOnlyList<StatusCountDto> ByStatus);
public record RankRowDto(string Label, int Completed);
public record BillingBucketDto(DateOnly Date, decimal Invoiced, decimal Collected);
public record BillingMetricsDto(decimal Invoiced, decimal Collected, decimal Outstanding, int BucketDays, IReadOnlyList<BillingBucketDto> Series);
public record MetricsDto(
    MetricsRange Range,
    DateOnly From,
    DateOnly To,
    AppointmentMetricsDto Appointments,
    IReadOnlyList<RankRowDto> Professionals,
    BillingMetricsDto? Billing);
