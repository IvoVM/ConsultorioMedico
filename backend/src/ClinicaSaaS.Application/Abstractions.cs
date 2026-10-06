using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application;

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
}

public interface ITimeZoneProvider
{
    TimeZoneInfo Zone { get; }
}

public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string value);
}

public interface ITokenService
{
    string CreatePlatformToken(Guid userId, string email, string name);
    string CreateTenantToken(Guid userId, string email, string name, string slug, IReadOnlyList<string> roles, bool mustChangePassword);
}

public interface ICatalogStore
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task AddAsync(Tenant tenant, CancellationToken ct);
    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface ITenantProvisioner
{
    Task<string> ProvisionAsync(string slug, CancellationToken ct);
}

public interface ITenantMigrator
{
    Task MigrateAllAsync(CancellationToken ct);
}

public interface IPlatformUserStore
{
    Task<PlatformLogin?> FindByEmailAsync(string email, CancellationToken ct);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
}

public record PlatformLogin(Guid Id, string Email, string Name);

public interface ITenantUserStore
{
    Task<TenantAccount?> FindByEmailAsync(string email, CancellationToken ct);
    Task<TenantAccount?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
    Task<TenantAccount> CreateAsync(string email, string password, string firstName, string lastName, TenantRole role, string? licenseNumber, Guid? specialtyId, bool mustChangePassword, CancellationToken ct);
    Task<IReadOnlyList<TenantAccount>> ListByRoleAsync(TenantRole role, Guid? specialtyId, CancellationToken ct);
    Task UpdateNameAsync(Guid id, string firstName, string lastName, CancellationToken ct);
}

public interface IOrganizationStore
{
    Task<IReadOnlyList<Location>> LocationsAsync(CancellationToken ct);
    Task<Location> AddLocationAsync(Location location, CancellationToken ct);
    Task<Location?> GetLocationAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<MedicalService>> MedicalServicesAsync(CancellationToken ct);
    Task<MedicalService> AddMedicalServiceAsync(MedicalService service, CancellationToken ct);
    Task<MedicalService?> GetMedicalServiceAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Specialty>> SpecialtiesAsync(CancellationToken ct);
    Task<Specialty?> GetSpecialtyByNameAsync(string name, CancellationToken ct);
    Task<Specialty> AddSpecialtyAsync(Specialty specialty, CancellationToken ct);
    Task<Specialty?> GetSpecialtyAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AppointmentType>> AppointmentTypesAsync(CancellationToken ct);
    Task<AppointmentType?> GetAppointmentTypeAsync(Guid id, CancellationToken ct);
    Task<AppointmentType> AddAppointmentTypeAsync(AppointmentType type, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IScheduleStore
{
    Task<IReadOnlyList<ScheduleBlock>> BlocksAsync(Guid professionalId, CancellationToken ct);
    Task ReplaceBlocksAsync(Guid professionalId, IReadOnlyList<ScheduleBlock> blocks, CancellationToken ct);
    Task<IReadOnlyList<ScheduleBlockout>> BlockoutsAsync(Guid professionalId, CancellationToken ct);
    Task<ScheduleBlockout> AddBlockoutAsync(ScheduleBlockout blockout, CancellationToken ct);
    Task DeleteBlockoutAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Appointment>> AppointmentsForDayAsync(Guid professionalId, DateOnly date, TimeZoneInfo zone, CancellationToken ct);
}

public interface IAppointmentStore
{
    Task<Appointment?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Appointment>> ForDayAsync(DateOnly date, Guid? professionalId, TimeZoneInfo zone, CancellationToken ct);
    Task<IReadOnlyList<Appointment>> ForPatientAsync(Guid patientId, CancellationToken ct);
    Task<Appointment> BookAsync(Appointment appointment, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IPatientStore
{
    Task<Patient?> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<Patient?> GetAsync(Guid id, CancellationToken ct);
    Task<Patient> AddAsync(Patient patient, CancellationToken ct);
}

public interface IMedicalRecordStore
{
    Task<IReadOnlyList<Patient>> PatientsAsync(CancellationToken ct);
    Task<IReadOnlyList<MedicalRecord>> ListAsync(CancellationToken ct);
    Task<MedicalRecord?> GetByPatientAsync(Guid patientId, CancellationToken ct);
    void Add(MedicalRecord record);
    Task SaveAsync(CancellationToken ct);
}

public interface IWaitlistStore
{
    Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct);
    Task<IReadOnlyList<WaitlistEntry>> PendingAsync(CancellationToken ct);
    Task<WaitlistEntry?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IClinicalStore
{
    Task<IReadOnlyList<Diagnosis>> DiagnosesAsync(CancellationToken ct);
    Task<Encounter?> GetEncounterByAppointmentAsync(Guid appointmentId, CancellationToken ct);
    Task<Encounter?> GetEncounterAsync(Guid id, CancellationToken ct);
    Task<Encounter> AddEncounterAsync(Encounter encounter, CancellationToken ct);
    Task ReplaceDiagnosesAsync(Guid encounterId, IReadOnlyList<Guid> diagnosisIds, CancellationToken ct);
    Task<IReadOnlyList<Encounter>> EncountersForPatientAsync(Guid patientId, CancellationToken ct);
    Task<IReadOnlyList<Diagnosis>> DiagnosesForEncounterAsync(Guid encounterId, CancellationToken ct);
    Task<Prescription> AddPrescriptionAsync(Prescription prescription, CancellationToken ct);
    Task<Prescription?> GetPrescriptionAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Prescription>> PrescriptionsForPatientAsync(Guid patientId, CancellationToken ct);
    Task<IReadOnlyList<PrescriptionItem>> PrescriptionItemsAsync(Guid prescriptionId, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IBillingStore
{
    Task<IReadOnlyList<Fee>> FeesAsync(CancellationToken ct);
    Task<Fee?> CurrentFeeAsync(Guid appointmentTypeId, DateOnly date, CancellationToken ct);
    Task<Fee> AddFeeAsync(Fee fee, CancellationToken ct);
    Task<Invoice?> GetInvoiceByAppointmentAsync(Guid appointmentId, CancellationToken ct);
    Task<Invoice> AddInvoiceAsync(Invoice invoice, CancellationToken ct);
    Task<Invoice?> GetInvoiceAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Invoice>> InvoicesAsync(CancellationToken ct);
    Task<IReadOnlyList<InvoiceItem>> InvoiceItemsAsync(Guid invoiceId, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IAuditStore
{
    Task RecordAsync(Guid? userId, string action, string entity, string? entityId, string? detail, CancellationToken ct);
    Task<IReadOnlyList<AuditEntry>> ListAsync(CancellationToken ct);
}
