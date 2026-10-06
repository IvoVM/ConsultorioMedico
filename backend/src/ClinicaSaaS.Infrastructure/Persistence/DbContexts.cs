using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClinicaSaaS.Infrastructure.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : IdentityDbContext<PlatformUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        FechaUtc.Aplicar(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Tenant>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Slug).HasMaxLength(40);
            e.Property(t => t.Nombre).HasMaxLength(120);
            e.Property(t => t.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.ConnectionStringProtegida).HasMaxLength(2000);
        });
    }
}

public class TenantDbContext(DbContextOptions<TenantDbContext> options)
    : IdentityDbContext<TenantUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<TipoTurno> TiposTurno => Set<TipoTurno>();
    public DbSet<AgendaSemanal> Agendas => Set<AgendaSemanal>();
    public DbSet<BloqueoAgenda> Bloqueos => Set<BloqueoAgenda>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<ListaEspera> ListaEspera => Set<ListaEspera>();
    public DbSet<Diagnostico> Diagnosticos => Set<Diagnostico>();
    public DbSet<Encuentro> Encuentros => Set<Encuentro>();
    public DbSet<EncuentroDiagnostico> EncuentroDiagnosticos => Set<EncuentroDiagnostico>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaItem> RecetaItems => Set<RecetaItem>();
    public DbSet<Arancel> Aranceles => Set<Arancel>();
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<ComprobanteItem> ComprobanteItems => Set<ComprobanteItem>();
    public DbSet<AuditoriaEntrada> Auditoria => Set<AuditoriaEntrada>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        FechaUtc.Aplicar(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TenantUser>(e =>
        {
            e.Property(u => u.Nombre).HasMaxLength(80);
            e.Property(u => u.Apellido).HasMaxLength(80);
            e.Property(u => u.Matricula).HasMaxLength(40);
            e.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
        });
        builder.Entity<Sede>(e =>
        {
            e.Property(s => s.Nombre).HasMaxLength(120);
            e.Property(s => s.Direccion).HasMaxLength(200);
        });
        builder.Entity<Servicio>(e => e.Property(s => s.Nombre).HasMaxLength(120));
        builder.Entity<Especialidad>(e =>
        {
            e.Property(s => s.Nombre).HasMaxLength(120);
            e.HasIndex(s => s.Nombre).IsUnique();
        });
        builder.Entity<TipoTurno>(e => e.Property(s => s.Nombre).HasMaxLength(120));
        builder.Entity<AgendaSemanal>(e =>
        {
            e.Property(a => a.Dia).HasConversion<string>().HasMaxLength(12);
            e.HasIndex(a => a.ProfesionalId);
        });
        builder.Entity<Paciente>(e =>
        {
            e.Property(p => p.Documento).HasMaxLength(20);
            e.Property(p => p.Telefono).HasMaxLength(30);
            e.HasIndex(p => p.UsuarioId).IsUnique();
        });
        builder.Entity<Turno>(e =>
        {
            e.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.MotivoConsulta).HasMaxLength(500);
            e.HasIndex(t => new { t.ProfesionalId, t.Inicio });
        });
        builder.Entity<ListaEspera>(e => e.Property(l => l.Estado).HasConversion<string>().HasMaxLength(20));
        builder.Entity<Diagnostico>(e =>
        {
            e.Property(d => d.Codigo).HasMaxLength(10);
            e.Property(d => d.Nombre).HasMaxLength(160);
            e.HasIndex(d => d.Codigo).IsUnique();
        });
        builder.Entity<Encuentro>(e =>
        {
            e.Property(x => x.Nota).HasMaxLength(4000);
            e.Property(x => x.TensionArterial).HasMaxLength(20);
            e.Property(x => x.Temperatura).HasPrecision(4, 1);
            e.Property(x => x.PesoKg).HasPrecision(5, 2);
            e.HasIndex(x => x.TurnoId).IsUnique();
        });
        builder.Entity<EncuentroDiagnostico>(e =>
        {
            e.HasKey(x => new { x.EncuentroId, x.DiagnosticoId });
            e.HasOne(x => x.Encuentro).WithMany(x => x.Diagnosticos).HasForeignKey(x => x.EncuentroId);
            e.HasOne(x => x.Diagnostico).WithMany().HasForeignKey(x => x.DiagnosticoId);
        });
        builder.Entity<Receta>(e => e.Property(r => r.Indicaciones).HasMaxLength(1000));
        builder.Entity<RecetaItem>(e =>
        {
            e.Property(i => i.Medicamento).HasMaxLength(120);
            e.Property(i => i.Dosis).HasMaxLength(60);
            e.Property(i => i.Frecuencia).HasMaxLength(60);
            e.Property(i => i.Duracion).HasMaxLength(60);
            e.HasOne(i => i.Receta).WithMany(r => r.Items).HasForeignKey(i => i.RecetaId);
        });
        builder.Entity<Arancel>(e => e.Property(a => a.Monto).HasPrecision(12, 2));
        builder.Entity<Comprobante>(e =>
        {
            e.Property(c => c.Total).HasPrecision(12, 2);
            e.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.MetodoPago).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(c => c.TurnoId).IsUnique();
        });
        builder.Entity<ComprobanteItem>(e =>
        {
            e.Property(i => i.Descripcion).HasMaxLength(160);
            e.Property(i => i.Importe).HasPrecision(12, 2);
            e.HasOne(i => i.Comprobante).WithMany(c => c.Items).HasForeignKey(i => i.ComprobanteId);
        });
        builder.Entity<AuditoriaEntrada>(e =>
        {
            e.Property(a => a.Accion).HasMaxLength(40);
            e.Property(a => a.Entidad).HasMaxLength(40);
            e.Property(a => a.Detalle).HasMaxLength(500);
            e.HasIndex(a => a.Fecha);
        });
    }
}

file static class FechaUtc
{
    public static void Aplicar(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<FechaUtcConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<FechaUtcNullableConverter>();
    }

    private sealed class FechaUtcConverter()
        : ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);

    private sealed class FechaUtcNullableConverter()
        : ValueConverter<DateTimeOffset?, DateTimeOffset?>(v => v.HasValue ? v.Value.ToUniversalTime() : v, v => v);
}
