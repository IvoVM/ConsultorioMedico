using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application;

public interface IUsuarioActual
{
    Guid? Id { get; }
    string? Rol { get; }
    string? TenantSlug { get; }
}

public interface ITenantActual
{
    string Slug { get; }
}

public interface IZonaHoraria
{
    TimeZoneInfo Zona { get; }
}

public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string value);
}

public interface ITokenService
{
    string CreatePlatformToken(Guid userId, string email, string nombre);
    string CreateTenantToken(Guid userId, string email, string nombre, string slug, RolTenant rol, bool debeCambiarClave);
}

public interface ICatalogStore
{
    Task<bool> ExisteSlugAsync(string slug, CancellationToken ct);
    Task AgregarAsync(Tenant tenant, CancellationToken ct);
    Task<IReadOnlyList<Tenant>> ListarAsync(CancellationToken ct);
    Task<Tenant?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
    Task<Tenant?> ObtenerPorSlugAsync(string slug, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface ITenantProvisioner
{
    Task<string> ProvisionAsync(string slug, CancellationToken ct);
}

public interface ITenantMigrator
{
    Task MigrateAllAsync(CancellationToken ct);
}

public interface IPlatformUserStore
{
    Task<PlatformLogin?> FindByEmailAsync(string email, CancellationToken ct);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
}

public record PlatformLogin(Guid Id, string Email, string Nombre);

public interface ITenantUserStore
{
    Task<UsuarioTenant?> FindByEmailAsync(string email, CancellationToken ct);
    Task<UsuarioTenant?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
    Task<UsuarioTenant> CreateAsync(string email, string password, string nombre, string apellido, RolTenant rol, string? matricula, Guid? especialidadId, bool debeCambiarClave, CancellationToken ct);
    Task<IReadOnlyList<UsuarioTenant>> ListarPorRolAsync(RolTenant rol, Guid? especialidadId, CancellationToken ct);
    Task ActualizarNombreAsync(Guid id, string nombre, string apellido, CancellationToken ct);
}

public interface IOrganizacionStore
{
    Task<IReadOnlyList<Sede>> SedesAsync(CancellationToken ct);
    Task<Sede> AgregarSedeAsync(Sede sede, CancellationToken ct);
    Task<Sede?> ObtenerSedeAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Servicio>> ServiciosAsync(CancellationToken ct);
    Task<Servicio> AgregarServicioAsync(Servicio servicio, CancellationToken ct);
    Task<Servicio?> ObtenerServicioAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Especialidad>> EspecialidadesAsync(CancellationToken ct);
    Task<Especialidad?> ObtenerEspecialidadPorNombreAsync(string nombre, CancellationToken ct);
    Task<Especialidad> AgregarEspecialidadAsync(Especialidad especialidad, CancellationToken ct);
    Task<Especialidad?> ObtenerEspecialidadAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TipoTurno>> TiposTurnoAsync(CancellationToken ct);
    Task<TipoTurno?> ObtenerTipoTurnoAsync(Guid id, CancellationToken ct);
    Task<TipoTurno> AgregarTipoTurnoAsync(TipoTurno tipo, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface IAgendaStore
{
    Task<IReadOnlyList<AgendaSemanal>> ListarAsync(Guid profesionalId, CancellationToken ct);
    Task ReemplazarAsync(Guid profesionalId, IReadOnlyList<AgendaSemanal> bloques, CancellationToken ct);
    Task<IReadOnlyList<BloqueoAgenda>> BloqueosAsync(Guid profesionalId, CancellationToken ct);
    Task<BloqueoAgenda> AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken ct);
    Task EliminarBloqueoAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Turno>> TurnosDelDiaAsync(Guid profesionalId, DateOnly fecha, TimeZoneInfo zona, CancellationToken ct);
}

public interface ITurnoStore
{
    Task<Turno?> ObtenerAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Turno>> DelDiaAsync(DateOnly fecha, Guid? profesionalId, TimeZoneInfo zona, CancellationToken ct);
    Task<IReadOnlyList<Turno>> DePacienteAsync(Guid pacienteId, CancellationToken ct);
    Task<Turno> ReservarAsync(Turno turno, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface IPacienteStore
{
    Task<Paciente?> PorUsuarioAsync(Guid usuarioId, CancellationToken ct);
    Task<Paciente?> ObtenerAsync(Guid id, CancellationToken ct);
    Task<Paciente> AgregarAsync(Paciente paciente, CancellationToken ct);
}

public interface IHistoriaMedicaStore
{
    Task<IReadOnlyList<Paciente>> PacientesAsync(CancellationToken ct);
    Task<IReadOnlyList<HistoriaMedica>> ListarAsync(CancellationToken ct);
    Task<HistoriaMedica?> PorPacienteAsync(Guid pacienteId, CancellationToken ct);
    void Agregar(HistoriaMedica historia);
    Task GuardarAsync(CancellationToken ct);
}

public interface IListaEsperaStore
{
    Task<ListaEspera> AgregarAsync(ListaEspera entrada, CancellationToken ct);
    Task<IReadOnlyList<ListaEspera>> PendientesAsync(CancellationToken ct);
    Task<ListaEspera?> ObtenerAsync(Guid id, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface IClinicaStore
{
    Task<IReadOnlyList<Diagnostico>> DiagnosticosAsync(CancellationToken ct);
    Task<Encuentro?> EncuentroPorTurnoAsync(Guid turnoId, CancellationToken ct);
    Task<Encuentro?> ObtenerEncuentroAsync(Guid id, CancellationToken ct);
    Task<Encuentro> AgregarEncuentroAsync(Encuentro encuentro, CancellationToken ct);
    Task ReemplazarDiagnosticosAsync(Guid encuentroId, IReadOnlyList<Guid> diagnosticoIds, CancellationToken ct);
    Task<IReadOnlyList<Encuentro>> EncuentrosDePacienteAsync(Guid pacienteId, CancellationToken ct);
    Task<IReadOnlyList<Diagnostico>> DiagnosticosDeEncuentroAsync(Guid encuentroId, CancellationToken ct);
    Task<Receta> AgregarRecetaAsync(Receta receta, CancellationToken ct);
    Task<Receta?> ObtenerRecetaAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Receta>> RecetasDePacienteAsync(Guid pacienteId, CancellationToken ct);
    Task<IReadOnlyList<RecetaItem>> ItemsRecetaAsync(Guid recetaId, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface IFacturacionStore
{
    Task<IReadOnlyList<Arancel>> ArancelesAsync(CancellationToken ct);
    Task<Arancel?> ArancelVigenteAsync(Guid tipoTurnoId, DateOnly fecha, CancellationToken ct);
    Task<Arancel> AgregarArancelAsync(Arancel arancel, CancellationToken ct);
    Task<Comprobante?> ComprobantePorTurnoAsync(Guid turnoId, CancellationToken ct);
    Task<Comprobante> AgregarComprobanteAsync(Comprobante comprobante, CancellationToken ct);
    Task<Comprobante?> ObtenerComprobanteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Comprobante>> ComprobantesAsync(CancellationToken ct);
    Task<IReadOnlyList<ComprobanteItem>> ItemsAsync(Guid comprobanteId, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface IAuditoriaStore
{
    Task RegistrarAsync(Guid? usuarioId, string accion, string entidad, string? entidadId, string? detalle, CancellationToken ct);
    Task<IReadOnlyList<AuditoriaEntrada>> ListarAsync(CancellationToken ct);
}
