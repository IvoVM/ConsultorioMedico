using ClinicaSaaS.Domain;
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
}

public interface ITenantUserStore
{
    IQueryable<TenantUser> Users { get; }
    Task<TenantAccount?> FindByEmailAsync(string email, CancellationToken ct);
    Task<TenantAccount?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
    Task<TenantAccount> CreateAsync(string email, string password, string firstName, string lastName, TenantRole role, bool mustChangePassword, CancellationToken ct);
    Task UpdateNameAsync(Guid id, string firstName, string lastName, CancellationToken ct);
}

public interface IEmployeeStore
{
    IQueryable<Employee> Employees { get; }
    Task<Employee?> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<Employee> AddAsync(Employee employee, CancellationToken ct);
    Task AssignSpecialtyAsync(Guid userId, Guid? specialtyId, CancellationToken ct);
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

public interface IAppointmentStore
{
    IQueryable<Appointment> Appointments { get; }
    Task<Appointment?> GetAsync(Guid id, CancellationToken ct);
    Task<Appointment> BookAsync(Appointment appointment, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IClientStore
{
    IQueryable<Client> Clients { get; }
    Task<Client?> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<Client?> GetAsync(Guid id, CancellationToken ct);
    Task<Client> AddAsync(Client client, CancellationToken ct);
}

public interface IMedicalRecordStore
{
    Task<MedicalRecord?> GetByClientAsync(Guid clientId, CancellationToken ct);
    void Add(MedicalRecord record);
    Task SaveAsync(CancellationToken ct);
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
    Task<Fee> AddFeeAsync(Fee fee, CancellationToken ct);
    Task<Invoice> AddInvoiceAsync(Invoice invoice, CancellationToken ct);
    Task<Invoice?> GetInvoiceAsync(Guid id, CancellationToken ct);
    IQueryable<Invoice> Invoices { get; }
    Task SaveAsync(CancellationToken ct);
}

public interface IAuditStore
{
    Task RecordAsync(Guid? userId, string action, string entity, string? entityId, string? detail, CancellationToken ct);
    IQueryable<AuditEntry> Entries { get; }
}
