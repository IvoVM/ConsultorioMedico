using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Route("api/salud")]
public class SaludController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { estado = "ok" });
}

[ApiController]
[Route("api/platform/auth")]
public class PlatformAuthController(PlatformAuthService auth) : ApiController
{
    [HttpPost("login")]
    public Task<IActionResult> Login(LoginCommand command, CancellationToken ct) =>
        Ejecutar(() => auth.LoginAsync(command, ct));
}

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/platform/tenants")]
public class PlatformTenantsController(TenantsService tenants) : ApiController
{
    [HttpGet]
    public Task<IActionResult> Listar(CancellationToken ct) => Ejecutar(() => tenants.ListarAsync(ct));

    [HttpPost]
    public Task<IActionResult> Crear(AltaTenantCommand command, CancellationToken ct) =>
        Ejecutar(() => tenants.CrearAsync(command, ct));

    [HttpPatch("{id:guid}/estado")]
    public Task<IActionResult> Estado(Guid id, CambiarEstadoTenantCommand command, CancellationToken ct) =>
        Ejecutar(() => tenants.CambiarEstadoAsync(id, command, ct));
}

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ApiController
{
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<IActionResult> Login(LoginCommand command, CancellationToken ct) =>
        Ejecutar(() => auth.LoginTenantAsync(command, ct));

    [AllowAnonymous]
    [HttpPost("registro")]
    public Task<IActionResult> Registro(RegistroPacienteCommand command, CancellationToken ct) =>
        Ejecutar(() => auth.RegistrarPacienteAsync(command, ct));
}

[ApiController]
[Authorize]
[Route("api")]
public class OrganizacionController(OrganizacionService organizacion) : ApiController
{
    [AllowAnonymous]
    [HttpGet("sedes")]
    public Task<IActionResult> Sedes(CancellationToken ct) => Ejecutar(() => organizacion.SedesAsync(ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPost("sedes")]
    public Task<IActionResult> CrearSede(GuardarSedeCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.CrearSedeAsync(command, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPut("sedes/{id:guid}")]
    public Task<IActionResult> ActualizarSede(Guid id, GuardarSedeCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.ActualizarSedeAsync(id, command, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpGet("servicios")]
    public Task<IActionResult> Servicios(CancellationToken ct) => Ejecutar(() => organizacion.ServiciosAsync(ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPost("servicios")]
    public Task<IActionResult> CrearServicio(GuardarServicioCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.CrearServicioAsync(command, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPut("servicios/{id:guid}")]
    public Task<IActionResult> ActualizarServicio(Guid id, GuardarServicioCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.ActualizarServicioAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("especialidades")]
    public Task<IActionResult> Especialidades(CancellationToken ct) =>
        Ejecutar(() => organizacion.EspecialidadesAsync(ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPost("especialidades")]
    public Task<IActionResult> CrearEspecialidad(GuardarEspecialidadCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.CrearEspecialidadAsync(command, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPut("especialidades/{id:guid}")]
    public Task<IActionResult> ActualizarEspecialidad(Guid id, GuardarEspecialidadCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.ActualizarEspecialidadAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("tipos-turno")]
    public Task<IActionResult> Tipos(CancellationToken ct) => Ejecutar(() => organizacion.TiposTurnoAsync(ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPost("tipos-turno")]
    public Task<IActionResult> CrearTipo(GuardarTipoTurnoCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.CrearTipoTurnoAsync(command, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPut("tipos-turno/{id:guid}")]
    public Task<IActionResult> ActualizarTipo(Guid id, GuardarTipoTurnoCommand command, CancellationToken ct) =>
        Ejecutar(() => organizacion.ActualizarTipoTurnoAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("profesionales")]
    public Task<IActionResult> Profesionales([FromQuery] Guid? especialidadId, CancellationToken ct) =>
        Ejecutar(() => organizacion.ProfesionalesAsync(especialidadId, ct));
}

[ApiController]
[Authorize(Roles = "AdminTenant")]
[Route("api/empleados")]
public class EmpleadosController(EmpleadosService empleados) : ApiController
{
    [HttpPost("importar")]
    public Task<IActionResult> Importar(ImportarEmpleadosCommand command, CancellationToken ct) =>
        Ejecutar(() => empleados.ImportarAsync(command, ct));
}

[ApiController]
[Authorize]
[Route("api")]
public class AgendaController(AgendaService agenda) : ApiController
{
    [Authorize(Roles = "AdminTenant")]
    [HttpGet("agendas")]
    public Task<IActionResult> Obtener([FromQuery] Guid profesionalId, CancellationToken ct) =>
        Ejecutar(() => agenda.ObtenerAgendaAsync(profesionalId, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPut("agendas")]
    public Task<IActionResult> Guardar(GuardarAgendaCommand command, CancellationToken ct) =>
        Ejecutar(() => agenda.GuardarAgendaAsync(command, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpGet("bloqueos")]
    public Task<IActionResult> Bloqueos([FromQuery] Guid profesionalId, CancellationToken ct) =>
        Ejecutar(() => agenda.BloqueosAsync(profesionalId, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPost("bloqueos")]
    public Task<IActionResult> CrearBloqueo(CrearBloqueoCommand command, CancellationToken ct) =>
        Ejecutar(() => agenda.CrearBloqueoAsync(command, ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpDelete("bloqueos/{id:guid}")]
    public Task<IActionResult> EliminarBloqueo(Guid id, CancellationToken ct) =>
        Ejecutar(() => agenda.EliminarBloqueoAsync(id, ct));

    [AllowAnonymous]
    [HttpGet("disponibilidad")]
    public Task<IActionResult> Disponibilidad([FromQuery] Guid sedeId, [FromQuery] Guid especialidadId, [FromQuery] Guid? profesionalId, [FromQuery] DateOnly fecha, CancellationToken ct) =>
        Ejecutar(() => agenda.DisponibilidadAsync(new DisponibilidadQuery(sedeId, especialidadId, profesionalId, fecha), ct));
}

[ApiController]
[Authorize]
[Route("api/turnos")]
public class TurnosController(AgendaService agenda) : ApiController
{
    [Authorize(Roles = "Paciente,Secretario,AdminTenant")]
    [HttpPost]
    public Task<IActionResult> Reservar(ReservarTurnoCommand command, CancellationToken ct) =>
        Ejecutar(() => agenda.ReservarAsync(command, ct));

    [Authorize(Roles = "Secretario,AdminTenant,Medico")]
    [HttpGet]
    public Task<IActionResult> DelDia([FromQuery] DateOnly fecha, [FromQuery] Guid? profesionalId, CancellationToken ct) =>
        Ejecutar(() => agenda.DelDiaAsync(fecha, profesionalId, ct));

    [Authorize(Roles = "Paciente")]
    [HttpGet("mios")]
    public Task<IActionResult> Mios(CancellationToken ct) => Ejecutar(() => agenda.MiosAsync(ct));

    [Authorize(Roles = "Paciente,Secretario,AdminTenant")]
    [HttpPost("{id:guid}/cancelar")]
    public Task<IActionResult> Cancelar(Guid id, CancellationToken ct) => Ejecutar(() => agenda.CancelarAsync(id, ct));

    [Authorize(Roles = "Secretario,AdminTenant")]
    [HttpPost("{id:guid}/admitir")]
    public Task<IActionResult> Admitir(Guid id, CancellationToken ct) => Ejecutar(() => agenda.AdmitirAsync(id, ct));

    [Authorize(Roles = "Secretario,AdminTenant")]
    [HttpPost("{id:guid}/reprogramar")]
    public Task<IActionResult> Reprogramar(Guid id, ReprogramarTurnoCommand command, CancellationToken ct) =>
        Ejecutar(() => agenda.ReprogramarAsync(id, command, ct));
}

[ApiController]
[Authorize]
[Route("api/lista-espera")]
public class ListaEsperaController(AgendaService agenda) : ApiController
{
    [Authorize(Roles = "Paciente,Secretario,AdminTenant")]
    [HttpPost]
    public Task<IActionResult> Crear(CrearListaEsperaCommand command, CancellationToken ct) =>
        Ejecutar(() => agenda.AnotarEsperaAsync(command, ct));

    [Authorize(Roles = "Secretario,AdminTenant")]
    [HttpGet]
    public Task<IActionResult> Listar(CancellationToken ct) => Ejecutar(() => agenda.ListaEsperaAsync(ct));

    [Authorize(Roles = "Secretario,AdminTenant")]
    [HttpPost("{id:guid}/asignar")]
    public Task<IActionResult> Asignar(Guid id, AsignarListaEsperaCommand command, CancellationToken ct) =>
        Ejecutar(() => agenda.AsignarEsperaAsync(id, command, ct));
}

[ApiController]
[Authorize]
[Route("api")]
public class ClinicaController(ClinicaService clinica) : ApiController
{
    [Authorize(Roles = "Medico,Secretario,AdminTenant")]
    [HttpGet("diagnosticos")]
    public Task<IActionResult> Diagnosticos(CancellationToken ct) => Ejecutar(() => clinica.DiagnosticosAsync(ct));

    [Authorize(Roles = "Medico")]
    [HttpPost("encuentros")]
    public Task<IActionResult> Guardar(GuardarEncuentroCommand command, CancellationToken ct) =>
        Ejecutar(() => clinica.GuardarEncuentroAsync(command, ct));

    [Authorize(Roles = "Medico")]
    [HttpPost("encuentros/{id:guid}/cerrar")]
    public Task<IActionResult> Cerrar(Guid id, CancellationToken ct) =>
        Ejecutar(() => clinica.CerrarEncuentroAsync(id, ct));

    [Authorize(Roles = "Medico")]
    [HttpPost("recetas")]
    public Task<IActionResult> Receta(CrearRecetaCommand command, CancellationToken ct) =>
        Ejecutar(() => clinica.CrearRecetaAsync(command, ct));

    [Authorize(Roles = "Medico,Paciente,Secretario,AdminTenant")]
    [HttpGet("recetas/{id:guid}")]
    public Task<IActionResult> ObtenerReceta(Guid id, CancellationToken ct) =>
        Ejecutar(() => clinica.ObtenerRecetaAsync(id, ct));

    [Authorize(Roles = "Paciente")]
    [HttpGet("recetas/mias")]
    public Task<IActionResult> Mias(CancellationToken ct) => Ejecutar(() => clinica.RecetasMiasAsync(ct));

    [Authorize(Roles = "Paciente,Medico,Secretario,AdminTenant")]
    [HttpGet("historia")]
    public Task<IActionResult> Historia([FromQuery] Guid? pacienteId, CancellationToken ct) =>
        Ejecutar(() => clinica.HistoriaAsync(pacienteId, ct));
}

[ApiController]
[Authorize(Roles = "AdminTenant")]
[Route("api/historias-medicas")]
public class HistoriasMedicasController(HistoriasMedicasService historias) : ApiController
{
    [HttpGet]
    public Task<IActionResult> Listar(CancellationToken ct) => Ejecutar(() => historias.ListarAsync(ct));

    [HttpGet("{pacienteId:guid}")]
    public Task<IActionResult> Obtener(Guid pacienteId, CancellationToken ct) =>
        Ejecutar(() => historias.ObtenerAsync(pacienteId, ct));

    [HttpPut("{pacienteId:guid}")]
    public Task<IActionResult> Guardar(Guid pacienteId, GuardarHistoriaMedicaCommand command, CancellationToken ct) =>
        Ejecutar(() => historias.GuardarAsync(pacienteId, command, ct));
}

[ApiController]
[Authorize]
[Route("api")]
public class FacturacionController(FacturacionService facturacion) : ApiController
{
    [Authorize(Roles = "AdminTenant")]
    [HttpGet("aranceles")]
    public Task<IActionResult> Aranceles(CancellationToken ct) => Ejecutar(() => facturacion.ArancelesAsync(ct));

    [Authorize(Roles = "AdminTenant")]
    [HttpPost("aranceles")]
    public Task<IActionResult> CrearArancel(CrearArancelCommand command, CancellationToken ct) =>
        Ejecutar(() => facturacion.CrearArancelAsync(command, ct));

    [Authorize(Roles = "AdminTenant,Secretario")]
    [HttpGet("comprobantes")]
    public Task<IActionResult> Comprobantes(CancellationToken ct) => Ejecutar(() => facturacion.ComprobantesAsync(ct));

    [Authorize(Roles = "AdminTenant,Secretario")]
    [HttpPost("comprobantes/{id:guid}/pagar")]
    public Task<IActionResult> Pagar(Guid id, PagarComprobanteCommand command, CancellationToken ct) =>
        Ejecutar(() => facturacion.PagarAsync(id, command, ct));
}

[ApiController]
[Authorize(Roles = "AdminTenant")]
[Route("api/auditoria")]
public class AuditoriaController(IAuditoriaStore auditoria) : ApiController
{
    [HttpGet]
    public Task<IActionResult> Listar(CancellationToken ct) => Ejecutar(async () =>
        (await auditoria.ListarAsync(ct))
            .Select(a => new AuditoriaDto(a.Id, a.UsuarioId, a.Accion, a.Entidad, a.EntidadId, a.Detalle, a.Fecha))
            .ToList());
}
