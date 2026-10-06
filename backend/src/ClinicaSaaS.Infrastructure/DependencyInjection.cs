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

        if (await users.Users.AnyAsync(ct))
            return;

        var slug = configuration["Clinic:Slug"]?.Trim().ToLowerInvariant();
        var email = configuration["Seed:AdminEmail"] ?? $"admin@{slug}.local";
        var user = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = configuration["Seed:AdminFirstName"] ?? "Admin",
            LastName = configuration["Seed:AdminLastName"] ?? "Inicial",
            Role = TenantRole.TenantAdmin
        };
        var result = await users.CreateAsync(user, configuration["Seed:AdminPassword"] ?? "Admin123!");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(' ', result.Errors.Select(e => e.Description)));
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
