using ClinicaSaaS.Application;
using ClinicaSaaS.Application.Mappings;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;
using ClinicaSaaS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure.Persistence;

public class TenantUserStore(UserManager<TenantUser> users) : ITenantUserStore
{
    public IQueryable<TenantUser> Users => users.Users.AsNoTracking();

    public Task<TenantAccount?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = users.NormalizeEmail(email);
        return Users.Where(u => u.NormalizedEmail == normalized).ToAccounts().FirstOrDefaultAsync(ct);
    }

    public Task<TenantAccount?> FindByIdAsync(Guid id, CancellationToken ct) =>
        Users.Where(u => u.Id == id).ToAccounts().FirstOrDefaultAsync(ct);

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is not null && await users.CheckPasswordAsync(user, password);
    }

    public async Task<TenantAccount> CreateAsync(string email, string password, string firstName, string lastName, TenantRole role, bool mustChangePassword, CancellationToken ct)
    {
        var user = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            MustChangePassword = mustChangePassword
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join(' ', result.Errors.Select(e => e.Description)));
        return Map(user);
    }

    public async Task UpdateNameAsync(Guid id, string firstName, string lastName, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("Usuario no encontrado.");
        user.FirstName = firstName;
        user.LastName = lastName;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join(' ', result.Errors.Select(e => e.Description)));
    }

    private static TenantAccount Map(TenantUser user) =>
        new(user.Id, user.Email ?? "", user.FirstName, user.LastName, user.Role, user.MustChangePassword);
}

public class EmployeeStore(TenantDbContext db) : IEmployeeStore
{
    public IQueryable<Employee> Employees => db.Employees.AsNoTracking();

    public Task<Employee?> GetByUserAsync(Guid userId, CancellationToken ct) =>
        db.Employees.FirstOrDefaultAsync(e => e.UserId == userId, ct);

    public async Task<Employee> AddAsync(Employee employee, CancellationToken ct)
    {
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return employee;
    }

    public async Task AssignSpecialtyAsync(Guid userId, Guid? specialtyId, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.UserId == userId, ct)
            ?? throw new NotFoundException("Empleado no encontrado.");
        employee.SpecialtyId = specialtyId;
        await db.SaveChangesAsync(ct);
    }
}

public class OrganizationStore(TenantDbContext db) : IOrganizationStore
{
    public IQueryable<Location> Locations => db.Locations.AsNoTracking();

    public async Task<Location> AddLocationAsync(Location location, CancellationToken ct)
    {
        db.Locations.Add(location);
        await db.SaveChangesAsync(ct);
        return location;
    }

    public Task<Location?> GetLocationAsync(Guid id, CancellationToken ct) => db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);

    public IQueryable<MedicalService> MedicalServices => db.MedicalServices.AsNoTracking();

    public async Task<MedicalService> AddMedicalServiceAsync(MedicalService service, CancellationToken ct)
    {
        db.MedicalServices.Add(service);
        await db.SaveChangesAsync(ct);
        return service;
    }

    public Task<MedicalService?> GetMedicalServiceAsync(Guid id, CancellationToken ct) => db.MedicalServices.FirstOrDefaultAsync(s => s.Id == id, ct);

    public IQueryable<Specialty> Specialties => db.Specialties.AsNoTracking();

    public Task<Specialty?> GetSpecialtyByNameAsync(string name, CancellationToken ct) =>
        db.Specialties.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower(), ct);

    public async Task<Specialty> AddSpecialtyAsync(Specialty specialty, CancellationToken ct)
    {
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync(ct);
        return specialty;
    }

    public Task<Specialty?> GetSpecialtyAsync(Guid id, CancellationToken ct) =>
        db.Specialties.FirstOrDefaultAsync(s => s.Id == id, ct);

    public IQueryable<AppointmentType> AppointmentTypes => db.AppointmentTypes.AsNoTracking();

    public Task<AppointmentType?> GetAppointmentTypeAsync(Guid id, CancellationToken ct) =>
        db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<AppointmentType> AddAppointmentTypeAsync(AppointmentType type, CancellationToken ct)
    {
        db.AppointmentTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return type;
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class ScheduleStore(TenantDbContext db) : IScheduleStore
{
    public IQueryable<ScheduleBlock> Blocks => db.ScheduleBlocks.AsNoTracking();

    public async Task ReplaceBlocksAsync(Guid professionalId, IReadOnlyList<ScheduleBlock> blocks, CancellationToken ct)
    {
        var current = await db.ScheduleBlocks.Where(b => b.ProfessionalId == professionalId).ToListAsync(ct);
        db.ScheduleBlocks.RemoveRange(current);
        db.ScheduleBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);
    }

    public IQueryable<ScheduleBlockout> Blockouts => db.ScheduleBlockouts.AsNoTracking();

    public async Task<ScheduleBlockout> AddBlockoutAsync(ScheduleBlockout blockout, CancellationToken ct)
    {
        db.ScheduleBlockouts.Add(blockout);
        await db.SaveChangesAsync(ct);
        return blockout;
    }

    public async Task DeleteBlockoutAsync(Guid id, CancellationToken ct)
    {
        var blockout = await db.ScheduleBlockouts.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException("Bloqueo no encontrado.");
        db.ScheduleBlockouts.Remove(blockout);
        await db.SaveChangesAsync(ct);
    }
}

public class AppointmentStore(TenantDbContext db) : IAppointmentStore
{
    public IQueryable<Appointment> Appointments => db.Appointments.AsNoTracking();

    public Task<Appointment?> GetAsync(Guid id, CancellationToken ct) => db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Appointment> BookAsync(Appointment appointment, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.Appointments
            .AsNoTracking()
            .Where(a => a.ProfessionalId == appointment.ProfessionalId && a.Start < appointment.End && a.End > appointment.Start)
            .Select(a => new BusyInterval { Start = a.Start, End = a.End, Status = a.Status })
            .ToListAsync(ct);
        var candidate = new TimeRange(appointment.Start, appointment.End);
        if (SchedulingRules.HasOverlap(existing.Select(a => (new TimeRange(a.Start, a.End), a.Status!.Value)), candidate))
            throw new ConflictException("El horario ya no está disponible.");
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return appointment;
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class ClientStore(TenantDbContext db) : IClientStore
{
    public IQueryable<Client> Clients => db.Clients.AsNoTracking();

    public Task<Client?> GetByUserAsync(Guid userId, CancellationToken ct) =>
        db.Clients.FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public Task<Client?> GetAsync(Guid id, CancellationToken ct) => db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Client> AddAsync(Client client, CancellationToken ct)
    {
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return client;
    }
}

public class MedicalRecordStore(TenantDbContext db) : IMedicalRecordStore
{
    public Task<MedicalRecord?> GetByClientAsync(Guid clientId, CancellationToken ct) =>
        db.MedicalRecords.FirstOrDefaultAsync(r => r.ClientId == clientId, ct);

    public void Add(MedicalRecord record) => db.MedicalRecords.Add(record);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class WaitlistStore(TenantDbContext db) : IWaitlistStore
{
    public IQueryable<WaitlistEntry> Entries => db.WaitlistEntries.AsNoTracking();

    public async Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct)
    {
        db.WaitlistEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public Task<WaitlistEntry?> GetAsync(Guid id, CancellationToken ct) => db.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class ClinicalStore(TenantDbContext db) : IClinicalStore
{
    public IQueryable<Diagnosis> Diagnoses => db.Diagnoses.AsNoTracking();

    public Task<Encounter?> GetEncounterByAppointmentAsync(Guid appointmentId, CancellationToken ct) =>
        db.Encounters.FirstOrDefaultAsync(e => e.AppointmentId == appointmentId, ct);

    public Task<Encounter?> GetEncounterAsync(Guid id, CancellationToken ct) =>
        db.Encounters.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Encounter> AddEncounterAsync(Encounter encounter, CancellationToken ct)
    {
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(ct);
        return encounter;
    }

    public async Task ReplaceDiagnosesAsync(Guid encounterId, IReadOnlyList<Guid> diagnosisIds, CancellationToken ct)
    {
        var current = await db.EncounterDiagnoses.Where(x => x.EncounterId == encounterId).ToListAsync(ct);
        db.EncounterDiagnoses.RemoveRange(current);
        foreach (var diagnosisId in diagnosisIds.Distinct())
            db.EncounterDiagnoses.Add(new EncounterDiagnosis { EncounterId = encounterId, DiagnosisId = diagnosisId });
    }

    public IQueryable<Encounter> Encounters => db.Encounters.AsNoTracking();

    public async Task<Prescription> AddPrescriptionAsync(Prescription prescription, CancellationToken ct)
    {
        foreach (var item in prescription.Items)
            item.PrescriptionId = prescription.Id;
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(ct);
        return prescription;
    }

    public IQueryable<Prescription> Prescriptions => db.Prescriptions.AsNoTracking();

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class BillingStore(TenantDbContext db) : IBillingStore
{
    public IQueryable<Fee> Fees => db.Fees.AsNoTracking();

    public async Task<Fee> AddFeeAsync(Fee fee, CancellationToken ct)
    {
        db.Fees.Add(fee);
        await db.SaveChangesAsync(ct);
        return fee;
    }

    public async Task<Invoice> AddInvoiceAsync(Invoice invoice, CancellationToken ct)
    {
        foreach (var item in invoice.Items)
            item.InvoiceId = invoice.Id;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public Task<Invoice?> GetInvoiceAsync(Guid id, CancellationToken ct) =>
        db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public IQueryable<Invoice> Invoices => db.Invoices.AsNoTracking();

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class AuditStore(TenantDbContext db) : IAuditStore
{
    public async Task RecordAsync(Guid? userId, string action, string entity, string? entityId, string? detail, CancellationToken ct)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Detail = detail,
            Timestamp = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public IQueryable<AuditEntry> Entries => db.AuditEntries.AsNoTracking();
}
