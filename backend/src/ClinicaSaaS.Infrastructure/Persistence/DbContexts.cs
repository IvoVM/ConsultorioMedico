using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClinicaSaaS.Infrastructure.Persistence;

public class TenantDbContext(DbContextOptions<TenantDbContext> options)
    : IdentityDbContext<TenantUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<MedicalService> MedicalServices => Set<MedicalService>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<ScheduleBlock> ScheduleBlocks => Set<ScheduleBlock>();
    public DbSet<ScheduleBlockout> ScheduleBlockouts => Set<ScheduleBlockout>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<EncounterDiagnosis> EncounterDiagnoses => Set<EncounterDiagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Fee> Fees => Set<Fee>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        UtcDates.Apply(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TenantUser>(e =>
        {
            e.ToTable("Users");
            e.Property(u => u.FirstName).HasMaxLength(80);
            e.Property(u => u.LastName).HasMaxLength(80);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.Ignore(u => u.Name);
            e.HasOne(u => u.Client).WithOne(c => c.User).HasForeignKey<Client>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(u => u.Employee).WithOne(x => x.User).HasForeignKey<Employee>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Location>(e =>
        {
            e.Property(l => l.Name).HasMaxLength(120);
            e.Property(l => l.Address).HasMaxLength(200);
        });
        builder.Entity<MedicalService>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(120);
            e.HasOne(s => s.Location).WithMany(l => l.Services).HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Specialty>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(120);
            e.HasIndex(s => s.Name).IsUnique();
        });
        builder.Entity<AppointmentType>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(120);
            e.HasOne(t => t.Specialty).WithMany(s => s.AppointmentTypes).HasForeignKey(t => t.SpecialtyId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<ScheduleBlock>(e =>
        {
            e.Property(b => b.Day).HasConversion<string>().HasMaxLength(12);
            e.HasIndex(b => b.ProfessionalId);
            e.HasOne(b => b.Professional).WithMany(u => u.ScheduleBlocks).HasForeignKey(b => b.ProfessionalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Location).WithMany(l => l.ScheduleBlocks).HasForeignKey(b => b.LocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.AppointmentType).WithMany(t => t.ScheduleBlocks).HasForeignKey(b => b.AppointmentTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ScheduleBlockout>(e =>
            e.HasOne(b => b.Professional).WithMany(u => u.ScheduleBlockouts).HasForeignKey(b => b.ProfessionalId).OnDelete(DeleteBehavior.Restrict));
        builder.Entity<Employee>(e =>
        {
            e.Property(x => x.LicenseNumber).HasMaxLength(40);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.Specialty).WithMany(s => s.Employees).HasForeignKey(x => x.SpecialtyId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<Client>(e =>
        {
            e.Property(c => c.DocumentNumber).HasMaxLength(20);
            e.Property(c => c.Phone).HasMaxLength(30);
            e.HasIndex(c => c.UserId).IsUnique();
        });
        builder.Entity<MedicalRecord>(e =>
        {
            e.Property(r => r.BloodType).HasMaxLength(3);
            e.Property(r => r.Allergies).HasMaxLength(1000);
            e.Property(r => r.PersonalHistory).HasMaxLength(2000);
            e.Property(r => r.FamilyHistory).HasMaxLength(2000);
            e.Property(r => r.CurrentMedication).HasMaxLength(1000);
            e.Property(r => r.Habits).HasMaxLength(1000);
            e.Property(r => r.HealthInsurance).HasMaxLength(120);
            e.Property(r => r.MemberNumber).HasMaxLength(40);
            e.Property(r => r.EmergencyContact).HasMaxLength(120);
            e.Property(r => r.EmergencyPhone).HasMaxLength(30);
            e.Property(r => r.Notes).HasMaxLength(2000);
            e.HasIndex(r => r.ClientId).IsUnique();
            e.HasOne(r => r.Client).WithOne(c => c.Record).HasForeignKey<MedicalRecord>(r => r.ClientId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Appointment>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.VisitReason).HasMaxLength(500);
            e.HasIndex(a => new { a.ProfessionalId, a.Start });
            e.HasOne(a => a.Client).WithMany(c => c.Appointments).HasForeignKey(a => a.ClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Professional).WithMany(u => u.Appointments).HasForeignKey(a => a.ProfessionalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Location).WithMany(l => l.Appointments).HasForeignKey(a => a.LocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.AppointmentType).WithMany(t => t.Appointments).HasForeignKey(a => a.AppointmentTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WaitlistEntry>(e =>
        {
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(w => w.Client).WithMany(c => c.WaitlistEntries).HasForeignKey(w => w.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Professional).WithMany(u => u.WaitlistEntries).HasForeignKey(w => w.ProfessionalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Location).WithMany(l => l.WaitlistEntries).HasForeignKey(w => w.LocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Specialty).WithMany(s => s.WaitlistEntries).HasForeignKey(w => w.SpecialtyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Diagnosis>(e =>
        {
            e.Property(d => d.Code).HasMaxLength(10);
            e.Property(d => d.Name).HasMaxLength(160);
            e.HasIndex(d => d.Code).IsUnique();
        });
        builder.Entity<Encounter>(e =>
        {
            e.Property(x => x.Note).HasMaxLength(4000);
            e.Property(x => x.BloodPressure).HasMaxLength(20);
            e.Property(x => x.Temperature).HasPrecision(4, 1);
            e.Property(x => x.WeightKg).HasPrecision(5, 2);
            e.HasIndex(x => x.AppointmentId).IsUnique();
            e.HasOne(x => x.Appointment).WithOne(a => a.Encounter).HasForeignKey<Encounter>(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Client).WithMany(c => c.Encounters).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Professional).WithMany(u => u.Encounters).HasForeignKey(x => x.ProfessionalId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<EncounterDiagnosis>(e =>
        {
            e.HasKey(x => new { x.EncounterId, x.DiagnosisId });
            e.HasOne(x => x.Encounter).WithMany(x => x.Diagnoses).HasForeignKey(x => x.EncounterId);
            e.HasOne(x => x.Diagnosis).WithMany(d => d.EncounterDiagnoses).HasForeignKey(x => x.DiagnosisId);
        });
        builder.Entity<Prescription>(e =>
        {
            e.Property(p => p.Instructions).HasMaxLength(1000);
            e.HasOne(p => p.Encounter).WithMany(x => x.Prescriptions).HasForeignKey(p => p.EncounterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Client).WithMany(c => c.Prescriptions).HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Professional).WithMany(u => u.Prescriptions).HasForeignKey(p => p.ProfessionalId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PrescriptionItem>(e =>
        {
            e.Property(i => i.Medication).HasMaxLength(120);
            e.Property(i => i.Dose).HasMaxLength(60);
            e.Property(i => i.Frequency).HasMaxLength(60);
            e.Property(i => i.Duration).HasMaxLength(60);
            e.HasOne(i => i.Prescription).WithMany(p => p.Items).HasForeignKey(i => i.PrescriptionId);
        });
        builder.Entity<Fee>(e =>
        {
            e.Property(f => f.Amount).HasPrecision(12, 2);
            e.HasOne(f => f.AppointmentType).WithMany(t => t.Fees).HasForeignKey(f => f.AppointmentTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Invoice>(e =>
        {
            e.Property(i => i.Total).HasPrecision(12, 2);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(i => i.AppointmentId).IsUnique();
            e.HasOne(i => i.Appointment).WithOne(a => a.Invoice).HasForeignKey<Invoice>(i => i.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Client).WithMany(c => c.Invoices).HasForeignKey(i => i.ClientId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<InvoiceItem>(e =>
        {
            e.Property(i => i.Description).HasMaxLength(160);
            e.Property(i => i.Amount).HasPrecision(12, 2);
            e.HasOne(i => i.Invoice).WithMany(i => i.Items).HasForeignKey(i => i.InvoiceId);
        });
        builder.Entity<AuditEntry>(e =>
        {
            e.Property(a => a.Action).HasMaxLength(40);
            e.Property(a => a.Entity).HasMaxLength(40);
            e.Property(a => a.Detail).HasMaxLength(500);
            e.HasIndex(a => a.Timestamp);
        });
        builder.Entity<RefreshSession>(e =>
        {
            e.Property(s => s.TokenHash).HasMaxLength(64);
            e.HasIndex(s => s.UserId);
            e.HasOne(s => s.User).WithMany(u => u.RefreshSessions).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

file static class UtcDates
{
    public static void Apply(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<NullableUtcDateConverter>();
    }

    private sealed class UtcDateConverter()
        : ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);

    private sealed class NullableUtcDateConverter()
        : ValueConverter<DateTimeOffset?, DateTimeOffset?>(v => v.HasValue ? v.Value.ToUniversalTime() : v, v => v);
}
