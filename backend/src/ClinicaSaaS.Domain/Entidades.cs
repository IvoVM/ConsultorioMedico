namespace ClinicaSaaS.Domain;

public class Tenant
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Nombre { get; set; } = "";
    public TipoTenant Tipo { get; set; }
    public EstadoTenant Estado { get; set; } = EstadoTenant.Activo;
    public string ConnectionStringProtegida { get; set; } = "";
    public DateTimeOffset CreadoEn { get; set; }
}

public class Sede
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public bool Activa { get; set; } = true;
}

public class Servicio
{
    public Guid Id { get; set; }
    public Guid SedeId { get; set; }
    public string Nombre { get; set; } = "";
}

public class Especialidad
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class TipoTurno
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public Guid? EspecialidadId { get; set; }
}

public class AgendaSemanal
{
    public Guid Id { get; set; }
    public Guid ProfesionalId { get; set; }
    public DayOfWeek Dia { get; set; }
    public TimeOnly HoraDesde { get; set; }
    public TimeOnly HoraHasta { get; set; }
    public Guid SedeId { get; set; }
    public Guid TipoTurnoId { get; set; }
}

public class BloqueoAgenda
{
    public Guid Id { get; set; }
    public Guid ProfesionalId { get; set; }
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }
    public string Motivo { get; set; } = "";
}

public class Paciente
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Documento { get; set; } = "";
    public DateOnly FechaNacimiento { get; set; }
    public string Telefono { get; set; } = "";
}

public class Turno
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfesionalId { get; set; }
    public Guid SedeId { get; set; }
    public Guid TipoTurnoId { get; set; }
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Reservado;
    public string? MotivoConsulta { get; set; }
}

public class ListaEspera
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? ProfesionalId { get; set; }
    public Guid SedeId { get; set; }
    public Guid EspecialidadId { get; set; }
    public EstadoListaEspera Estado { get; set; } = EstadoListaEspera.Pendiente;
    public DateTimeOffset CreadoEn { get; set; }
    public string? Notas { get; set; }
}

public class Diagnostico
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public class Encuentro
{
    public Guid Id { get; set; }
    public Guid TurnoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfesionalId { get; set; }
    public string? Nota { get; set; }
    public string? TensionArterial { get; set; }
    public int? FrecuenciaCardiaca { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? PesoKg { get; set; }
    public bool Cerrado { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public List<EncuentroDiagnostico> Diagnosticos { get; set; } = [];
}

public class EncuentroDiagnostico
{
    public Guid EncuentroId { get; set; }
    public Guid DiagnosticoId { get; set; }
    public Encuentro? Encuentro { get; set; }
    public Diagnostico? Diagnostico { get; set; }
}

public class Receta
{
    public Guid Id { get; set; }
    public Guid EncuentroId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfesionalId { get; set; }
    public string? Indicaciones { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public List<RecetaItem> Items { get; set; } = [];
}

public class RecetaItem
{
    public Guid Id { get; set; }
    public Guid RecetaId { get; set; }
    public string Medicamento { get; set; } = "";
    public string Dosis { get; set; } = "";
    public string Frecuencia { get; set; } = "";
    public string Duracion { get; set; } = "";
    public Receta? Receta { get; set; }
}

public class Arancel
{
    public Guid Id { get; set; }
    public Guid TipoTurnoId { get; set; }
    public decimal Monto { get; set; }
    public DateOnly VigenteDesde { get; set; }
}

public class Comprobante
{
    public Guid Id { get; set; }
    public Guid TurnoId { get; set; }
    public Guid PacienteId { get; set; }
    public decimal Total { get; set; }
    public EstadoComprobante Estado { get; set; } = EstadoComprobante.Pendiente;
    public MetodoPago? MetodoPago { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? PagadoEn { get; set; }
    public List<ComprobanteItem> Items { get; set; } = [];
}

public class ComprobanteItem
{
    public Guid Id { get; set; }
    public Guid ComprobanteId { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Importe { get; set; }
    public Comprobante? Comprobante { get; set; }
}

public class AuditoriaEntrada
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public DateTimeOffset Fecha { get; set; }
}
