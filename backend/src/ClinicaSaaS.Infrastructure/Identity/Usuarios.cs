using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using Microsoft.AspNetCore.Identity;

namespace ClinicaSaaS.Infrastructure.Identity;

public class PlatformUser : IdentityUser<Guid>
{
    public string Nombre { get; set; } = "";
}

public class TenantUser : IdentityUser<Guid>
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Matricula { get; set; }
    public Guid? EspecialidadId { get; set; }
    public RolTenant Rol { get; set; }
    public bool DebeCambiarClave { get; set; }
}

public sealed class CurrentTenant : ITenantActual
{
    public bool IsResolved { get; private set; }
    public string Slug { get; private set; } = "";
    public string? ConnectionString { get; private set; }
    public EstadoTenant Estado { get; private set; }

    public void Resolve(string slug, string connectionString, EstadoTenant estado)
    {
        Slug = slug;
        ConnectionString = connectionString;
        Estado = estado;
        IsResolved = true;
    }
}
