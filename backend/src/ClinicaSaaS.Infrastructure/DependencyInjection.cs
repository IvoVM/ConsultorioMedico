using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Infrastructure;

public sealed class ConfiguredClinic : ICurrentTenant
{
    public ConfiguredClinic(IConfiguration configuration)
    {
        var slug = configuration["Clinic:Slug"]?.Trim().ToLowerInvariant();
        if (!SlugRules.IsValid(slug))
            throw new InvalidOperationException("Clinic:Slug inválido. Usá minúsculas, números y guiones.");
        Slug = slug!;
        var name = configuration["Clinic:Name"]?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Falta Clinic:Name.");
        Name = name;
    }

    public string Slug { get; }
    public string Name { get; }
}

public class ConfiguredTimeZone(IConfiguration configuration) : ITimeZoneProvider
{
    public TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["TimeZone"] ?? "America/Argentina/Buenos_Aires");
}

public class ClinicSeeder(TenantDbContext db, UserManager<TenantUser> users, IConfiguration configuration)
{
    public static readonly (string Code, string Name)[] SeedDiagnoses =
    [
        ("J06.9", "Infección respiratoria aguda"),
        ("I10", "Hipertensión esencial"),
        ("E11.9", "Diabetes mellitus tipo 2"),
        ("M54.5", "Lumbago"),
        ("J45.9", "Asma"),
        ("K21.0", "Reflujo gastroesofágico"),
        ("N39.0", "Infección urinaria"),
        ("J00", "Resfrío común")
    ];

    public async Task SeedAsync(CancellationToken ct)
    {
        if (!await db.Diagnoses.AnyAsync(ct))
        {
            db.Diagnoses.AddRange(SeedDiagnoses.Select(d => new Diagnosis
            {
                Id = Guid.NewGuid(),
                Code = d.Code,
                Name = d.Name
            }));
            await db.SaveChangesAsync(ct);
        }

        var slug = configuration["Clinic:Slug"]?.Trim().ToLowerInvariant();
        var password = configuration["Seed:AdminPassword"] ?? "Admin123!";
        var adminEmail = configuration["Seed:AdminEmail"] ?? $"admin@{slug}.local";
        await EnsureUserAsync(
            adminEmail,
            password,
            configuration["Seed:AdminFirstName"] ?? "Admin",
            configuration["Seed:AdminLastName"] ?? "Inicial",
            TenantRole.TenantAdmin,
            null,
            ct);

        if (!await db.Locations.AnyAsync(ct))
        {
            var clinicName = configuration["Clinic:Name"]?.Trim();
            db.Locations.Add(new Location
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrEmpty(clinicName) ? "Consultorio" : clinicName,
                Address = "",
                IsActive = true
            });
            await db.SaveChangesAsync(ct);
        }

        if (slug != "demo")
            return;

        await EnsureUserAsync("secretaria@demo.local", password, "Laura", "Benítez", TenantRole.Secretary, null, ct);
        await EnsureUserAsync("medico@demo.local", password, "Martín", "Ríos", TenantRole.Doctor, "MN 12345", ct);
        var patient = await EnsureUserAsync("paciente@demo.local", password, "Ana", "Pérez", TenantRole.Patient, null, ct);
        if (!await db.Patients.AnyAsync(p => p.UserId == patient.Id, ct))
        {
            db.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(),
                UserId = patient.Id,
                DocumentNumber = "30111222",
                BirthDate = new DateOnly(1990, 4, 12),
                Phone = "1112345678"
            });
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<TenantUser> EnsureUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        TenantRole role,
        string? licenseNumber,
        CancellationToken ct)
    {
        if (await users.FindByEmailAsync(email) is { } existing)
            return existing;

        var user = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            LicenseNumber = licenseNumber
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(' ', result.Errors.Select(e => e.Description)));
        return user;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Clinic")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Clinic.");
        services.AddDbContext<TenantDbContext>(options => options.UseNpgsql(connection));
        services.AddSingleton<ICurrentTenant, ConfiguredClinic>();

        services.AddIdentityCore<TenantUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<TenantDbContext>();

        services.AddScoped<ITenantUserStore, TenantUserStore>();
        services.AddScoped<IRefreshSessionStore, RefreshSessionStore>();
        services.AddScoped<IOrganizationStore, OrganizationStore>();
        services.AddScoped<IScheduleStore, ScheduleStore>();
        services.AddScoped<IAppointmentStore, AppointmentStore>();
        services.AddScoped<IPatientStore, PatientStore>();
        services.AddScoped<IMedicalRecordStore, MedicalRecordStore>();
        services.AddScoped<IWaitlistStore, WaitlistStore>();
        services.AddScoped<IClinicalStore, ClinicalStore>();
        services.AddScoped<IBillingStore, BillingStore>();
        services.AddScoped<IAuditStore, AuditStore>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ITimeZoneProvider, ConfiguredTimeZone>();
        services.AddScoped<ClinicSeeder>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
