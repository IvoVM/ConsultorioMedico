using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ClinicaSaaS.Infrastructure;

public class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("ClinicaSaaS.TenantConnection");

    public string Protect(string value) => _protector.Protect(value);
    public string Unprotect(string value) => _protector.Unprotect(value);
}

public class ConfiguredTimeZone(IConfiguration configuration) : ITimeZoneProvider
{
    public TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["TimeZone"] ?? "America/Argentina/Buenos_Aires");
}

public class TenantProvisioner(IConfiguration configuration) : ITenantProvisioner
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

    public async Task<string> ProvisionAsync(string slug, CancellationToken ct)
    {
        if (!SlugRules.IsValid(slug))
            throw new BusinessRuleException("Slug inválido.");
        var database = SlugRules.DatabaseName(slug);
        if (!System.Text.RegularExpressions.Regex.IsMatch(database, "^tenant_[a-z0-9_]+$"))
            throw new BusinessRuleException("Nombre de base inválido.");

        var admin = configuration.GetConnectionString("Admin")
            ?? throw new InvalidOperationException("Falta la connection string Admin.");
        await using (var connection = new NpgsqlConnection(admin))
        {
            await connection.OpenAsync(ct);
            await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
            exists.Parameters.AddWithValue("name", database);
            if (await exists.ExecuteScalarAsync(ct) is null)
            {
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
                await create.ExecuteNonQueryAsync(ct);
            }
        }

        var builder = new NpgsqlConnectionStringBuilder(admin) { Database = database };
        var tenantConnection = builder.ConnectionString;
        var options = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(tenantConnection).Options;
        await using var db = new TenantDbContext(options);
        await db.Database.MigrateAsync(ct);
        if (!await db.Diagnoses.AnyAsync(ct))
        {
            db.Diagnoses.AddRange(SeedDiagnoses.Select(d => new Diagnosis
            {
                Id = Guid.NewGuid(),
                Code = d.Code,
                Name = d.Name
            }));
        }

        if (!await db.Users.AnyAsync(ct))
        {
            var email = $"admin@{slug}.local";
            var adminUser = new TenantUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Inicial",
                Role = TenantRole.TenantAdmin,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            adminUser.PasswordHash = new PasswordHasher<TenantUser>().HashPassword(adminUser, "Admin123!");
            db.Users.Add(adminUser);
        }

        await db.SaveChangesAsync(ct);

        return tenantConnection;
    }
}

public class TenantMigrator(CatalogDbContext catalog, ISecretProtector protector) : ITenantMigrator
{
    public async Task MigrateAllAsync(CancellationToken ct)
    {
        var tenants = await catalog.Tenants.AsNoTracking().ToListAsync(ct);
        foreach (var tenant in tenants)
        {
            var connection = protector.Unprotect(tenant.ProtectedConnectionString);
            var options = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connection).Options;
            await using var db = new TenantDbContext(options);
            await db.Database.MigrateAsync(ct);
        }
    }
}

public class SuperAdminSeeder(UserManager<PlatformUser> users, IConfiguration configuration)
{
    public async Task SeedAsync(CancellationToken ct)
    {
        var email = configuration["Seed:SuperAdminEmail"] ?? "admin@clinica.local";
        if (await users.FindByEmailAsync(email) is not null)
            return;
        var user = new PlatformUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Name = configuration["Seed:SuperAdminName"] ?? "Administrador"
        };
        var result = await users.CreateAsync(user, configuration["Seed:SuperAdminPassword"] ?? "Admin123!");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(' ', result.Errors.Select(e => e.Description)));
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Catalog")));

        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());

        services.AddDbContext<TenantDbContext>((sp, options) =>
        {
            var current = sp.GetRequiredService<CurrentTenant>();
            if (!current.IsResolved || string.IsNullOrWhiteSpace(current.ConnectionString))
                throw new InvalidOperationException("No hay un consultorio resuelto para esta operación.");
            options.UseNpgsql(current.ConnectionString);
        });

        services.AddIdentityCore<PlatformUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<CatalogDbContext>();

        services.AddIdentityCore<TenantUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<TenantDbContext>();

        services.AddScoped<ICatalogStore, CatalogStore>();
        services.AddScoped<IPlatformUserStore, PlatformUserStore>();
        services.AddScoped<ITenantUserStore, TenantUserStore>();
        services.AddScoped<IOrganizationStore, OrganizationStore>();
        services.AddScoped<IScheduleStore, ScheduleStore>();
        services.AddScoped<IAppointmentStore, AppointmentStore>();
        services.AddScoped<IPatientStore, PatientStore>();
        services.AddScoped<IMedicalRecordStore, MedicalRecordStore>();
        services.AddScoped<IWaitlistStore, WaitlistStore>();
        services.AddScoped<IClinicalStore, ClinicalStore>();
        services.AddScoped<IBillingStore, BillingStore>();
        services.AddScoped<IAuditStore, AuditStore>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ITimeZoneProvider, ConfiguredTimeZone>();
        services.AddScoped<ITenantProvisioner, TenantProvisioner>();
        services.AddScoped<ITenantMigrator, TenantMigrator>();
        services.AddScoped<SuperAdminSeeder>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
