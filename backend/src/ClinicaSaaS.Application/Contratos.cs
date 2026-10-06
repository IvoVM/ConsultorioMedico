using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application;

public record UsuarioTenant(
    Guid Id,
    string Email,
    string Nombre,
    string Apellido,
    RolTenant Rol,
    string? Matricula,
    Guid? EspecialidadId,
    bool DebeCambiarClave);

public record AltaTenantCommand(string Slug, string Nombre, TipoTenant Tipo);
public record TenantDto(Guid Id, string Slug, string Nombre, TipoTenant Tipo, EstadoTenant Estado, DateTimeOffset CreadoEn);
public record CambiarEstadoTenantCommand(EstadoTenant Estado);

public record LoginCommand(string Email, string Password);
public record TokenDto(string Token, string Email, string Nombre, string Rol, string? TenantSlug, bool DebeCambiarClave);

public record RegistroPacienteCommand(
    string Email,
    string Password,
    string Nombre,
    string Apellido,
    string Documento,
    DateOnly FechaNacimiento,
    string Telefono);

public record SedeDto(Guid Id, string Nombre, string Direccion, bool Activa);
public record GuardarSedeCommand(string Nombre, string Direccion);
public record ServicioDto(Guid Id, Guid SedeId, string Nombre);
public record GuardarServicioCommand(Guid SedeId, string Nombre);
public record EspecialidadDto(Guid Id, string Nombre);
public record GuardarEspecialidadCommand(string Nombre);
public record TipoTurnoDto(Guid Id, string Nombre, int DuracionMinutos, Guid? EspecialidadId);
public record GuardarTipoTurnoCommand(string Nombre, int DuracionMinutos, Guid? EspecialidadId);
public record ProfesionalDto(Guid Id, string Nombre, string Apellido, string Email, string? Matricula, Guid? EspecialidadId);

public record ImportarEmpleadosCommand(string Csv);
public record EmpleadoCreadoDto(int Fila, string Email, string ClaveTemporal, RolTenant Rol);
public record FilaRechazadaDto(int Fila, string Motivo);
public record ImportacionEmpleadosDto(IReadOnlyList<EmpleadoCreadoDto> Creados, IReadOnlyList<FilaRechazadaDto> Rechazados);

public record BloqueAgendaDto(Guid Id, DayOfWeek Dia, TimeOnly HoraDesde, TimeOnly HoraHasta, Guid SedeId, Guid TipoTurnoId);
public record GuardarAgendaCommand(Guid ProfesionalId, IReadOnlyList<BloqueAgendaDto> Bloques);
public record BloqueoDto(Guid Id, Guid ProfesionalId, DateTimeOffset Inicio, DateTimeOffset Fin, string Motivo);
public record CrearBloqueoCommand(Guid ProfesionalId, DateTimeOffset Inicio, DateTimeOffset Fin, string Motivo);

public record HuecoDto(Guid ProfesionalId, string Profesional, Guid SedeId, Guid TipoTurnoId, string TipoTurno, DateTimeOffset Inicio, DateTimeOffset Fin);
public record DisponibilidadQuery(Guid SedeId, Guid EspecialidadId, Guid? ProfesionalId, DateOnly Fecha);

public record ReservarTurnoCommand(Guid? PacienteId, Guid ProfesionalId, Guid SedeId, Guid TipoTurnoId, DateTimeOffset Inicio, string? MotivoConsulta);
public record TurnoDto(
    Guid Id,
    Guid PacienteId,
    string Paciente,
    Guid ProfesionalId,
    string Profesional,
    Guid SedeId,
    string Sede,
    Guid TipoTurnoId,
    string TipoTurno,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    EstadoTurno Estado,
    string? MotivoConsulta);
public record ReprogramarTurnoCommand(DateTimeOffset Inicio);

public record ListaEsperaDto(
    Guid Id,
    Guid PacienteId,
    string Paciente,
    Guid? ProfesionalId,
    Guid SedeId,
    Guid EspecialidadId,
    EstadoListaEspera Estado,
    DateTimeOffset CreadoEn,
    string? Notas);
public record CrearListaEsperaCommand(Guid? PacienteId, Guid? ProfesionalId, Guid SedeId, Guid EspecialidadId, string? Notas);
public record AsignarListaEsperaCommand(DateTimeOffset Inicio, Guid TipoTurnoId);

public record DiagnosticoDto(Guid Id, string Codigo, string Nombre);
public record GuardarEncuentroCommand(
    Guid TurnoId,
    string? Nota,
    string? TensionArterial,
    int? FrecuenciaCardiaca,
    decimal? Temperatura,
    decimal? PesoKg,
    IReadOnlyList<Guid> DiagnosticoIds);
public record EncuentroDto(
    Guid Id,
    Guid TurnoId,
    Guid PacienteId,
    Guid ProfesionalId,
    string? Nota,
    string? TensionArterial,
    int? FrecuenciaCardiaca,
    decimal? Temperatura,
    decimal? PesoKg,
    bool Cerrado,
    DateTimeOffset CreadoEn,
    IReadOnlyList<DiagnosticoDto> Diagnosticos);

public record RecetaItemDto(string Medicamento, string Dosis, string Frecuencia, string Duracion);
public record CrearRecetaCommand(Guid EncuentroId, string? Indicaciones, IReadOnlyList<RecetaItemDto> Items);
public record RecetaDto(
    Guid Id,
    Guid EncuentroId,
    Guid PacienteId,
    Guid ProfesionalId,
    string Profesional,
    string? Indicaciones,
    DateTimeOffset CreadoEn,
    IReadOnlyList<RecetaItemDto> Items);

public record HistoriaDto(PacienteResumenDto Paciente, IReadOnlyList<EncuentroDto> Encuentros, IReadOnlyList<RecetaDto> Recetas);
public record PacienteResumenDto(Guid Id, string Nombre, string Documento, DateOnly FechaNacimiento, string Telefono);

public record HistoriaMedicaDto(
    Guid PacienteId,
    string Nombre,
    string Apellido,
    string Email,
    string Documento,
    DateOnly FechaNacimiento,
    string Telefono,
    string? GrupoSanguineo,
    string? Alergias,
    string? AntecedentesPersonales,
    string? AntecedentesFamiliares,
    string? MedicacionHabitual,
    string? Habitos,
    string? ObraSocial,
    string? NumeroAfiliado,
    string? ContactoEmergencia,
    string? TelefonoEmergencia,
    string? Observaciones,
    DateTimeOffset? ActualizadoEn);
public record GuardarHistoriaMedicaCommand(
    string Nombre,
    string Apellido,
    string Documento,
    DateOnly FechaNacimiento,
    string Telefono,
    string? GrupoSanguineo,
    string? Alergias,
    string? AntecedentesPersonales,
    string? AntecedentesFamiliares,
    string? MedicacionHabitual,
    string? Habitos,
    string? ObraSocial,
    string? NumeroAfiliado,
    string? ContactoEmergencia,
    string? TelefonoEmergencia,
    string? Observaciones);

public record ArancelDto(Guid Id, Guid TipoTurnoId, string TipoTurno, decimal Monto, DateOnly VigenteDesde);
public record CrearArancelCommand(Guid TipoTurnoId, decimal Monto, DateOnly VigenteDesde);
public record ComprobanteItemDto(string Descripcion, decimal Importe);
public record ComprobanteDto(
    Guid Id,
    Guid TurnoId,
    Guid PacienteId,
    string Paciente,
    decimal Total,
    EstadoComprobante Estado,
    MetodoPago? MetodoPago,
    DateTimeOffset CreadoEn,
    DateTimeOffset? PagadoEn,
    IReadOnlyList<ComprobanteItemDto> Items);
public record PagarComprobanteCommand(MetodoPago Metodo);

public record AuditoriaDto(Guid Id, Guid? UsuarioId, string Accion, string Entidad, string? EntidadId, string? Detalle, DateTimeOffset Fecha);
