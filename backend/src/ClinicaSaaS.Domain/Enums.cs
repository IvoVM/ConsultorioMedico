namespace ClinicaSaaS.Domain;

public enum TipoTenant
{
    Consultorio,
    Hospital
}

public enum EstadoTenant
{
    Activo,
    Suspendido,
    Baja
}

public enum RolTenant
{
    AdminTenant,
    Medico,
    Secretario,
    Paciente
}

public enum EstadoTurno
{
    Reservado,
    Admitido,
    EnCurso,
    Completado,
    Cancelado,
    Ausente
}

public enum EstadoListaEspera
{
    Pendiente,
    Ofrecido,
    Aceptado,
    Cancelado
}

public enum MetodoPago
{
    Efectivo,
    Transferencia,
    Tarjeta
}

public enum EstadoComprobante
{
    Pendiente,
    Pagado,
    Anulado
}
