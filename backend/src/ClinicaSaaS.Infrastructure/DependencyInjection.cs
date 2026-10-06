using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace ClinicaSaaS.Infrastructure;

public class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("ClinicaSaaS.TenantConnection");

    public string Protect(string value) => _protector.Protect(value);
    public string Unprotect(string value) => _protector.Unprotect(value);
}

public class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public string CreatePlatformToken(Guid userId, string email, string nombre) =>
        Create(userId, email, nombre, "SuperAdmin", null, false);

    public string CreateTenantToken(Guid userId, string email, string nombre, string slug, RolTenant rol, bool debeCambiarClave) =>
        Create(userId, email, nombre, rol.ToString(), slug, debeCambiarClave);

    private string Create(Guid userId, string email, string nombre, string rol, string? slug, bool debeCambiarClave)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, nombre),
            new(ClaimTypes.Role, rol),
            new("debe_cambiar_clave", debeCambiarClave ? "true" : "false")
        };
        if (slug is not null)
            claims.Add(new Claim("tenant_slug", slug));

        var minutes = int.TryParse(configuration["Jwt:ExpiresMinutes"], out var value) ? value : 720;
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class ConfiguredTimeZone(IConfiguration configuration) : IZonaHoraria
{
    public TimeZoneInfo Zona { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["ZonaHoraria"] ?? "America/Argentina/Buenos_Aires");
}

public class TenantProvisioner(IConfiguration configuration) : ITenantProvisioner
{
    public static readonly (string Codigo, string Nombre)[] DiagnosticosSemilla =
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
        if (!SlugRules.EsValido(slug))
            throw new ReglaNegocioException("Slug inválido.");
        var database = SlugRules.NombreBase(slug);
        if (!System.Text.RegularExpressions.Regex.IsMatch(database, "^tenant_[a-z0-9_]+$"))
            throw new ReglaNegocioException("Nombre de base inválido.");

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
        if (!await db.Diagnosticos.AnyAsync(ct))
        {
            db.Diagnosticos.AddRange(DiagnosticosSemilla.Select(d => new Diagnostico
            {
                Id = Guid.NewGuid(),
                Codigo = d.Codigo,
                Nombre = d.Nombre
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
                Nombre = "Admin",
                Apellido = "Inicial",
                Rol = RolTenant.AdminTenant,
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
            var connection = protector.Unprotect(tenant.ConnectionStringProtegida);
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
            Nombre = configuration["Seed:SuperAdminNombre"] ?? "Administrador"
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
        services.AddScoped<ITenantActual>(sp => sp.GetRequiredService<CurrentTenant>());

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
        services.AddScoped<ClinicaStores>();
        services.AddScoped<IOrganizacionStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddScoped<IAgendaStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddScoped<ITurnoStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddScoped<IPacienteStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddScoped<IListaEsperaStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddScoped<IClinicaStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddScoped<IFacturacionStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddScoped<IAuditoriaStore>(sp => sp.GetRequiredService<ClinicaStores>());
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IZonaHoraria, ConfiguredTimeZone>();
        services.AddScoped<ITenantProvisioner, TenantProvisioner>();
        services.AddScoped<ITenantMigrator, TenantMigrator>();
        services.AddScoped<SuperAdminSeeder>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
