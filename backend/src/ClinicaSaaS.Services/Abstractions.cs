using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;
using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Services;

public interface ICurrentUser
{
    Guid? Id { get; }
    string? Role { get; }
    IReadOnlyList<string> Roles { get; }
    string? TenantSlug { get; }
}

public interface ICurrentTenant
{
    string Slug { get; }
    string Name { get; }
}

public interface ITimeZoneProvider
{
    TimeZoneInfo Zone { get; }
}

public interface ITokenService
{
    string CreateTenantToken(Guid userId, string email, string name, string slug, IReadOnlyList<string> roles, bool mustChangePassword);
}

public record RefreshGrant(Guid UserId, string RawToken, DateTimeOffset Expires);

public interface IRefreshSessionStore
{
    Task<RefreshGrant> IssueAsync(Guid userId, CancellationToken ct);
    Task<RefreshGrant?> RotateAsync(string? rawToken, CancellationToken ct);
    Task RevokeAsync(string? rawToken, CancellationToken ct);
    Task RevokeUserAsync(Guid userId, CancellationToken ct);
}

public interface ITenantUserStore
{
    IQueryable<TenantUser> Users { get; }
    Task<TenantAccount?> FindByEmailAsync(string email, CancellationToken ct);
    Task<TenantAccount?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
    Task<TenantAccount> CreateAsync(string email, string password, string firstName, string lastName, TenantRole role, bool mustChangePassword, CancellationToken ct);
    Task UpdateNameAsync(Guid id, string firstName, string lastName, CancellationToken ct);
    Task UpdateProfileAsync(Guid id, string firstName, string lastName, string email, TenantRole role, CancellationToken ct);
    Task SetPasswordAsync(Guid id, string password, CancellationToken ct);
    Task SetDocumentPasswordAsync(Guid id, string password, CancellationToken ct);
}

public interface IEmployeeStore
{
    IQueryable<Employee> Employees { get; }
    Task<Employee?> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<Employee> AddAsync(Employee employee, CancellationToken ct);
    Task AssignSpecialtyAsync(Guid userId, Guid? specialtyId, CancellationToken ct);
    Task UpdateAsync(Guid userId, string? licenseNumber, Guid? specialtyId, string documentNumber, CancellationToken ct);
    Task SetActiveAsync(Guid userId, bool active, CancellationToken ct);
    Task SetInsurersAsync(Guid userId, IReadOnlyList<Guid> insurerIds, CancellationToken ct);
}

public interface IOrganizationStore
{
    IQueryable<Location> Locations { get; }
    Task<Location> AddLocationAsync(Location location, CancellationToken ct);
    Task<Location?> GetLocationAsync(Guid id, CancellationToken ct);
    IQueryable<MedicalService> MedicalServices { get; }
    Task<MedicalService> AddMedicalServiceAsync(MedicalService service, CancellationToken ct);
    Task<MedicalService?> GetMedicalServiceAsync(Guid id, CancellationToken ct);
    IQueryable<Specialty> Specialties { get; }
    Task<Specialty?> GetSpecialtyByNameAsync(string name, CancellationToken ct);
    Task<Specialty> AddSpecialtyAsync(Specialty specialty, CancellationToken ct);
    Task<Specialty?> GetSpecialtyAsync(Guid id, CancellationToken ct);
    IQueryable<AppointmentType> AppointmentTypes { get; }
    Task<AppointmentType?> GetAppointmentTypeAsync(Guid id, CancellationToken ct);
    Task<AppointmentType> AddAppointmentTypeAsync(AppointmentType type, CancellationToken ct);
    IQueryable<Insurer> Insurers { get; }
    Task<Insurer> AddInsurerAsync(Insurer insurer, CancellationToken ct);
    Task<Insurer?> GetInsurerAsync(Guid id, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IScheduleStore
{
    IQueryable<ScheduleBlock> Blocks { get; }
    Task ReplaceBlocksAsync(Guid professionalId, IReadOnlyList<ScheduleBlock> blocks, CancellationToken ct);
    IQueryable<ScheduleBlockout> Blockouts { get; }
    Task<ScheduleBlockout> AddBlockoutAsync(ScheduleBlockout blockout, CancellationToken ct);
    Task DeleteBlockoutAsync(Guid id, CancellationToken ct);
}

// Runs a use case in one database transaction, so a failure halfway leaves nothing half-saved.
public interface IUnitOfWork
{
    Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct);
}

public interface IAppointmentStore
{
    IQueryable<Appointment> Appointments { get; }
    IQueryable<AppointmentChange> Changes { get; }
    Task<Appointment?> GetAsync(Guid id, CancellationToken ct);
    Task<AppointmentChange?> GetChangeAsync(Guid appointmentId, Guid changeId, CancellationToken ct);
    void AddChange(AppointmentChange change);
    void RemoveChange(AppointmentChange change);
    Task<Appointment> BookAsync(Appointment appointment, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IClientStore
{
    IQueryable<Client> Clients { get; }
    Task<Client?> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<Client?> GetAsync(Guid id, CancellationToken ct);
    Task<Client> AddAsync(Client client, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IMedicalRecordStore
{
    Task<MedicalRecord?> GetByClientAsync(Guid clientId, CancellationToken ct);
    void Add(MedicalRecord record);
    Task SaveAsync(CancellationToken ct);
}

public interface IBookingRequestStore
{
    IQueryable<BookingRequest> Entries { get; }
    Task<BookingRequest> AddAsync(BookingRequest request, CancellationToken ct);
    Task<BookingRequest?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task LinkDocumentAsync(string documentNumber, Guid clientId, CancellationToken ct);
}

public interface IWaitlistStore
{
    IQueryable<WaitlistEntry> Entries { get; }
    Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct);
    Task<WaitlistEntry?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IClinicalStore
{
    IQueryable<Diagnosis> Diagnoses { get; }
    Task<Encounter?> GetEncounterByAppointmentAsync(Guid appointmentId, CancellationToken ct);
    Task<Encounter?> GetEncounterAsync(Guid id, CancellationToken ct);
    Task<Encounter> AddEncounterAsync(Encounter encounter, CancellationToken ct);
    Task ReplaceDiagnosesAsync(Guid encounterId, IReadOnlyList<Guid> diagnosisIds, CancellationToken ct);
    IQueryable<Encounter> Encounters { get; }
    Task<Prescription> AddPrescriptionAsync(Prescription prescription, CancellationToken ct);
    IQueryable<Prescription> Prescriptions { get; }
    Task SaveAsync(CancellationToken ct);
}

public interface IBillingStore
{
    IQueryable<Fee> Fees { get; }
    IQueryable<FeeRow> FeeRows(DateOnly today);
    Task<Fee?> GetCurrentFeeAsync(Guid appointmentTypeId, DateOnly today, string? payer, CancellationToken ct);
    Task RemoveFeesEffectiveFromAsync(Guid appointmentTypeId, DateOnly day, string? payer, Guid? keepId, CancellationToken ct);
    Task<Fee> AddFeeAsync(Fee fee, CancellationToken ct);
    Task<Invoice> AddInvoiceAsync(Invoice invoice, CancellationToken ct);
    // Tracked, with its items and payments.
    Task<Invoice?> GetInvoiceAsync(Guid id, CancellationToken ct);
    IQueryable<Invoice> Invoices { get; }
    IQueryable<InvoicePayment> Payments { get; }
    void AddPayment(InvoicePayment payment);
    void AddItem(InvoiceItem item);
    void RemoveItems(IEnumerable<InvoiceItem> items);
    IQueryable<Receipt> Receipts { get; }
    void AddReceipt(Receipt receipt);
    Task<Receipt?> GetReceiptAsync(Guid id, CancellationToken ct);
    // Tracked invoices with their items, payments and plans.
    Task<List<Invoice>> GetInvoicesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    IQueryable<PaymentPlan> Plans { get; }
    void AddPlan(PaymentPlan plan);
    IQueryable<CashSession> CashSessions { get; }
    Task<CashSession?> GetCashSessionAsync(Guid id, CancellationToken ct);
    void AddCashSession(CashSession session);
    void AddCashMovement(CashMovement movement);
    IQueryable<CardSettlement> Settlements { get; }
    IQueryable<CardSettlementLine> SettlementLines { get; }
    Task<CardSettlementLine?> GetSettlementLineAsync(Guid id, CancellationToken ct);
    void AddSettlement(CardSettlement settlement);
    Task SaveAsync(CancellationToken ct);
}

// The card processor that settles card payments (Prisma, Fiserv…), or a simulation of it.
public interface ICardProcessor
{
    bool Simulated { get; }
    Task<IReadOnlyList<SettlementLineData>> SettlementAsync(CardBrand brand, DateOnly from, DateOnly to, CancellationToken ct);
}

public record SettlementLineData(string Batch, string Coupon, int Installments, DateOnly ChargedOn, DateOnly PaidOn, decimal Gross, decimal Fee);

// The tax authority that numbers and authorizes fiscal vouchers (AFIP/ARCA web services, or a simulation).
public interface IFiscalAuthority
{
    Task<FiscalAuthorization> AuthorizeAsync(FiscalRequest request, CancellationToken ct);
}

public record FiscalRequest(string TaxId, int PointOfSale, FiscalVoucherType Type, decimal Amount, string CustomerDocument, DateOnly Date);
public record FiscalAuthorization(int Number, string Cae, DateOnly CaeDueDate, bool Simulated);

public interface IClinicSettingsStore
{
    // Returns the tracked settings row, creating it from the configured clinic the first time.
    Task<ClinicSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IAuditStore
{
    Task RecordAsync(Guid? userId, string action, string entity, string? entityId, string? detail, CancellationToken ct);
    IQueryable<AuditEntry> Entries { get; }
}
