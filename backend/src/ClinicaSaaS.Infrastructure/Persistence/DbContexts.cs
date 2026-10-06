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
    public DbSet<Patient> Patients => Set<Patient>();
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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        UtcDates.Apply(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TenantUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(80);
            e.Property(u => u.LastName).HasMaxLength(80);
            e.Property(u => u.LicenseNumber).HasMaxLength(40);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });
        builder.Entity<Location>(e =>
        {
            e.Property(l => l.Name).HasMaxLength(120);
            e.Property(l => l.Address).HasMaxLength(200);
        });
        builder.Entity<MedicalService>(e => e.Property(s => s.Name).HasMaxLength(120));
        builder.Entity<Specialty>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(120);
            e.HasIndex(s => s.Name).IsUnique();
        });
        builder.Entity<AppointmentType>(e => e.Property(t => t.Name).HasMaxLength(120));
        builder.Entity<ScheduleBlock>(e =>
        {
            e.Property(b => b.Day).HasConversion<string>().HasMaxLength(12);
            e.HasIndex(b => b.ProfessionalId);
        });
        builder.Entity<Patient>(e =>
        {
            e.Property(p => p.DocumentNumber).HasMaxLength(20);
            e.Property(p => p.Phone).HasMaxLength(30);
            e.HasIndex(p => p.UserId).IsUnique();
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
            e.HasIndex(r => r.PatientId).IsUnique();
            e.HasOne<Patient>().WithOne().HasForeignKey<MedicalRecord>(r => r.PatientId);
        });
        builder.Entity<Appointment>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.VisitReason).HasMaxLength(500);
            e.HasIndex(a => new { a.ProfessionalId, a.Start });
        });
        builder.Entity<WaitlistEntry>(e => e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20));
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
        });
        builder.Entity<EncounterDiagnosis>(e =>
        {
            e.HasKey(x => new { x.EncounterId, x.DiagnosisId });
            e.HasOne(x => x.Encounter).WithMany(x => x.Diagnoses).HasForeignKey(x => x.EncounterId);
            e.HasOne(x => x.Diagnosis).WithMany().HasForeignKey(x => x.DiagnosisId);
        });
        builder.Entity<Prescription>(e => e.Property(p => p.Instructions).HasMaxLength(1000));
        builder.Entity<PrescriptionItem>(e =>
        {
            e.Property(i => i.Medication).HasMaxLength(120);
            e.Property(i => i.Dose).HasMaxLength(60);
            e.Property(i => i.Frequency).HasMaxLength(60);
            e.Property(i => i.Duration).HasMaxLength(60);
            e.HasOne(i => i.Prescription).WithMany(p => p.Items).HasForeignKey(i => i.PrescriptionId);
        });
        builder.Entity<Fee>(e => e.Property(f => f.Amount).HasPrecision(12, 2));
        builder.Entity<Invoice>(e =>
        {
            e.Property(i => i.Total).HasPrecision(12, 2);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(i => i.AppointmentId).IsUnique();
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
