using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure.Persistence;

public class TenantUserStore(UserManager<TenantUser> users) : ITenantUserStore
{
    public async Task<TenantAccount?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<TenantAccount?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        return user is null ? null : Map(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is not null && await users.CheckPasswordAsync(user, password);
    }

    public async Task<TenantAccount> CreateAsync(string email, string password, string firstName, string lastName, TenantRole role, string? licenseNumber, Guid? specialtyId, bool mustChangePassword, CancellationToken ct)
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
            LicenseNumber = licenseNumber,
            SpecialtyId = specialtyId,
            MustChangePassword = mustChangePassword
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join(' ', result.Errors.Select(e => e.Description)));
        return Map(user);
    }

    public async Task<IReadOnlyList<TenantAccount>> ListByRoleAsync(TenantRole role, Guid? specialtyId, CancellationToken ct)
    {
        var query = users.Users.Where(u => u.Role == role);
        if (specialtyId is Guid id)
            query = query.Where(u => u.SpecialtyId == id);
        var list = await query.OrderBy(u => u.LastName).ToListAsync(ct);
        return list.Select(Map).ToList();
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
        new(user.Id, user.Email ?? "", user.FirstName, user.LastName, user.Role, user.LicenseNumber, user.SpecialtyId, user.MustChangePassword);
}

public class OrganizationStore(TenantDbContext db) : IOrganizationStore
{
    public async Task<IReadOnlyList<Location>> LocationsAsync(CancellationToken ct) =>
        await db.Locations.OrderBy(l => l.Name).ToListAsync(ct);

    public async Task<Location> AddLocationAsync(Location location, CancellationToken ct)
    {
        db.Locations.Add(location);
        await db.SaveChangesAsync(ct);
        return location;
    }

    public Task<Location?> GetLocationAsync(Guid id, CancellationToken ct) => db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<MedicalService>> MedicalServicesAsync(CancellationToken ct) =>
        await db.MedicalServices.OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<MedicalService> AddMedicalServiceAsync(MedicalService service, CancellationToken ct)
    {
        db.MedicalServices.Add(service);
        await db.SaveChangesAsync(ct);
        return service;
    }

    public Task<MedicalService?> GetMedicalServiceAsync(Guid id, CancellationToken ct) => db.MedicalServices.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Specialty>> SpecialtiesAsync(CancellationToken ct) =>
        await db.Specialties.OrderBy(s => s.Name).ToListAsync(ct);

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

    public async Task<IReadOnlyList<AppointmentType>> AppointmentTypesAsync(CancellationToken ct) =>
        await db.AppointmentTypes.OrderBy(t => t.Name).ToListAsync(ct);

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
    public async Task<IReadOnlyList<ScheduleBlock>> BlocksAsync(Guid professionalId, CancellationToken ct) =>
        await db.ScheduleBlocks.Where(b => b.ProfessionalId == professionalId).OrderBy(b => b.Day).ThenBy(b => b.StartTime).ToListAsync(ct);

    public async Task ReplaceBlocksAsync(Guid professionalId, IReadOnlyList<ScheduleBlock> blocks, CancellationToken ct)
    {
        var current = await db.ScheduleBlocks.Where(b => b.ProfessionalId == professionalId).ToListAsync(ct);
        db.ScheduleBlocks.RemoveRange(current);
        db.ScheduleBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ScheduleBlockout>> BlockoutsAsync(Guid professionalId, CancellationToken ct) =>
        await db.ScheduleBlockouts.Where(b => b.ProfessionalId == professionalId).OrderBy(b => b.Start).ToListAsync(ct);

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

    public Task<IReadOnlyList<Appointment>> AppointmentsForDayAsync(Guid professionalId, DateOnly date, TimeZoneInfo zone, CancellationToken ct) =>
        AppointmentQueries.ForDayAsync(db, date, professionalId, zone, ct);
}

public class AppointmentStore(TenantDbContext db) : IAppointmentStore
{
    public Task<Appointment?> GetAsync(Guid id, CancellationToken ct) => db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<IReadOnlyList<Appointment>> ForDayAsync(DateOnly date, Guid? professionalId, TimeZoneInfo zone, CancellationToken ct) =>
        AppointmentQueries.ForDayAsync(db, date, professionalId, zone, ct);

    public async Task<IReadOnlyList<Appointment>> ForPatientAsync(Guid patientId, CancellationToken ct) =>
        await db.Appointments.Where(a => a.PatientId == patientId).ToListAsync(ct);

    public async Task<Appointment> BookAsync(Appointment appointment, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.Appointments
            .Where(a => a.ProfessionalId == appointment.ProfessionalId && a.Start < appointment.End && a.End > appointment.Start)
            .ToListAsync(ct);
        var candidate = new TimeRange(appointment.Start, appointment.End);
        if (SchedulingRules.HasOverlap(existing.Select(a => (new TimeRange(a.Start, a.End), a.Status)), candidate))
            throw new ConflictException("El horario ya no está disponible.");
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return appointment;
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class PatientStore(TenantDbContext db) : IPatientStore
{
    public Task<Patient?> GetByUserAsync(Guid userId, CancellationToken ct) =>
        db.Patients.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<Patient?> GetAsync(Guid id, CancellationToken ct) => db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Patient> AddAsync(Patient patient, CancellationToken ct)
    {
        db.Patients.Add(patient);
        await db.SaveChangesAsync(ct);
        return patient;
    }
}

public class MedicalRecordStore(TenantDbContext db) : IMedicalRecordStore
{
    public async Task<IReadOnlyList<Patient>> PatientsAsync(CancellationToken ct) =>
        await db.Patients.ToListAsync(ct);

    public async Task<IReadOnlyList<MedicalRecord>> ListAsync(CancellationToken ct) =>
        await db.MedicalRecords.ToListAsync(ct);

    public Task<MedicalRecord?> GetByPatientAsync(Guid patientId, CancellationToken ct) =>
        db.MedicalRecords.FirstOrDefaultAsync(r => r.PatientId == patientId, ct);

    public void Add(MedicalRecord record) => db.MedicalRecords.Add(record);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class WaitlistStore(TenantDbContext db) : IWaitlistStore
{
    public async Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct)
    {
        db.WaitlistEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<IReadOnlyList<WaitlistEntry>> PendingAsync(CancellationToken ct) =>
        await db.WaitlistEntries.Where(w => w.Status == WaitlistStatus.Pending || w.Status == WaitlistStatus.Offered)
            .OrderBy(w => w.CreatedAt).ToListAsync(ct);

    public Task<WaitlistEntry?> GetAsync(Guid id, CancellationToken ct) => db.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class ClinicalStore(TenantDbContext db) : IClinicalStore
{
    public async Task<IReadOnlyList<Diagnosis>> DiagnosesAsync(CancellationToken ct) =>
        await db.Diagnoses.OrderBy(d => d.Code).ToListAsync(ct);

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

    public async Task<IReadOnlyList<Encounter>> EncountersForPatientAsync(Guid patientId, CancellationToken ct) =>
        await db.Encounters.Where(e => e.PatientId == patientId).ToListAsync(ct);

    public async Task<IReadOnlyList<Diagnosis>> DiagnosesForEncounterAsync(Guid encounterId, CancellationToken ct) =>
        await db.EncounterDiagnoses.Where(x => x.EncounterId == encounterId).Select(x => x.Diagnosis!).ToListAsync(ct);

    public async Task<Prescription> AddPrescriptionAsync(Prescription prescription, CancellationToken ct)
    {
        foreach (var item in prescription.Items)
            item.PrescriptionId = prescription.Id;
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(ct);
        return prescription;
    }

    public async Task<Prescription?> GetPrescriptionAsync(Guid id, CancellationToken ct) =>
        await db.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Prescription>> PrescriptionsForPatientAsync(Guid patientId, CancellationToken ct) =>
        await db.Prescriptions.Include(p => p.Items).Where(p => p.PatientId == patientId).ToListAsync(ct);

    public async Task<IReadOnlyList<PrescriptionItem>> PrescriptionItemsAsync(Guid prescriptionId, CancellationToken ct) =>
        await db.PrescriptionItems.Where(i => i.PrescriptionId == prescriptionId).ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class BillingStore(TenantDbContext db) : IBillingStore
{
    public async Task<IReadOnlyList<Fee>> FeesAsync(CancellationToken ct) =>
        await db.Fees.OrderByDescending(f => f.EffectiveFrom).ToListAsync(ct);

    public async Task<Fee?> CurrentFeeAsync(Guid appointmentTypeId, DateOnly date, CancellationToken ct) =>
        await db.Fees.Where(f => f.AppointmentTypeId == appointmentTypeId && f.EffectiveFrom <= date)
            .OrderByDescending(f => f.EffectiveFrom).FirstOrDefaultAsync(ct);

    public async Task<Fee> AddFeeAsync(Fee fee, CancellationToken ct)
    {
        db.Fees.Add(fee);
        await db.SaveChangesAsync(ct);
        return fee;
    }

    public Task<Invoice?> GetInvoiceByAppointmentAsync(Guid appointmentId, CancellationToken ct) =>
        db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.AppointmentId == appointmentId, ct);

    public async Task<Invoice> AddInvoiceAsync(Invoice invoice, CancellationToken ct)
    {
        foreach (var item in invoice.Items)
            item.InvoiceId = invoice.Id;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public Task<Invoice?> GetInvoiceAsync(Guid id, CancellationToken ct) =>
        db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Invoice>> InvoicesAsync(CancellationToken ct) =>
        await db.Invoices.Include(i => i.Items).OrderByDescending(i => i.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<InvoiceItem>> InvoiceItemsAsync(Guid invoiceId, CancellationToken ct) =>
        await db.InvoiceItems.Where(i => i.InvoiceId == invoiceId).ToListAsync(ct);

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

    public async Task<IReadOnlyList<AuditEntry>> ListAsync(CancellationToken ct) =>
        await db.AuditEntries.OrderByDescending(a => a.Timestamp).Take(200).ToListAsync(ct);
}

file static class AppointmentQueries
{
    public static async Task<IReadOnlyList<Appointment>> ForDayAsync(TenantDbContext db, DateOnly date, Guid? professionalId, TimeZoneInfo zone, CancellationToken ct)
    {
        var start = SchedulingRules.Combine(date, TimeOnly.MinValue, zone);
        var end = start.AddDays(1);
        var query = db.Appointments.Where(a => a.Start >= start && a.Start < end);
        if (professionalId is Guid id)
            query = query.Where(a => a.ProfessionalId == id);
        return await query.ToListAsync(ct);
    }
}
