using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class AgendaService(
    IAgendaStore agendas,
    IOrganizacionStore organizacion,
    ITenantUserStore usuarios,
    ITurnoStore turnos,
    IPacienteStore pacientes,
    IListaEsperaStore listaEspera,
    IZonaHoraria zonaHoraria,
    IUsuarioActual actual,
    IAuditoriaStore auditoria,
    IValidator<ReservarTurnoCommand> reservaValidator,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<BloqueAgendaDto>> ObtenerAgendaAsync(Guid profesionalId, CancellationToken ct) =>
        (await agendas.ListarAsync(profesionalId, ct))
            .Select(b => new BloqueAgendaDto(b.Id, b.Dia, b.HoraDesde, b.HoraHasta, b.SedeId, b.TipoTurnoId))
            .ToList();

    public async Task<IReadOnlyList<BloqueAgendaDto>> GuardarAgendaAsync(GuardarAgendaCommand command, CancellationToken ct)
    {
        foreach (var bloque in command.Bloques)
        {
            if (bloque.HoraHasta <= bloque.HoraDesde)
                throw new ReglaNegocioException("El horario de fin debe ser posterior al de inicio.");
            _ = await organizacion.ObtenerTipoTurnoAsync(bloque.TipoTurnoId, ct)
                ?? throw new NoEncontradoException("Tipo de turno no encontrado.");
        }

        var entidades = command.Bloques.Select(b => new AgendaSemanal
        {
            Id = Guid.NewGuid(),
            ProfesionalId = command.ProfesionalId,
            Dia = b.Dia,
            HoraDesde = b.HoraDesde,
            HoraHasta = b.HoraHasta,
            SedeId = b.SedeId,
            TipoTurnoId = b.TipoTurnoId
        }).ToList();
        await agendas.ReemplazarAsync(command.ProfesionalId, entidades, ct);
        await auditoria.RegistrarAsync(actual.Id, "agenda", "AgendaSemanal", command.ProfesionalId.ToString(), $"{entidades.Count} bloques", ct);
        return entidades.Select(b => new BloqueAgendaDto(b.Id, b.Dia, b.HoraDesde, b.HoraHasta, b.SedeId, b.TipoTurnoId)).ToList();
    }

    public async Task<IReadOnlyList<BloqueoDto>> BloqueosAsync(Guid profesionalId, CancellationToken ct) =>
        (await agendas.BloqueosAsync(profesionalId, ct))
            .Select(b => new BloqueoDto(b.Id, b.ProfesionalId, b.Inicio, b.Fin, b.Motivo))
            .ToList();

    public async Task<BloqueoDto> CrearBloqueoAsync(CrearBloqueoCommand command, CancellationToken ct)
    {
        if (command.Fin <= command.Inicio)
            throw new ReglaNegocioException("El bloqueo debe tener un rango válido.");
        var bloqueo = await agendas.AgregarBloqueoAsync(new BloqueoAgenda
        {
            Id = Guid.NewGuid(),
            ProfesionalId = command.ProfesionalId,
            Inicio = command.Inicio,
            Fin = command.Fin,
            Motivo = command.Motivo.Trim()
        }, ct);
        return new BloqueoDto(bloqueo.Id, bloqueo.ProfesionalId, bloqueo.Inicio, bloqueo.Fin, bloqueo.Motivo);
    }

    public Task EliminarBloqueoAsync(Guid id, CancellationToken ct) => agendas.EliminarBloqueoAsync(id, ct);

    public async Task<IReadOnlyList<HuecoDto>> DisponibilidadAsync(DisponibilidadQuery query, CancellationToken ct)
    {
        var especialidadId = query.EspecialidadId == Guid.Empty ? (Guid?)null : query.EspecialidadId;
        var profesionales = await usuarios.ListarPorRolAsync(RolTenant.Medico, especialidadId, ct);
        if (query.ProfesionalId is Guid filtro)
            profesionales = profesionales.Where(p => p.Id == filtro).ToList();

        var tipos = await organizacion.TiposTurnoAsync(ct);
        var resultado = new List<HuecoDto>();
        foreach (var profesional in profesionales)
        {
            var bloques = (await agendas.ListarAsync(profesional.Id, ct))
                .Where(b => b.Dia == query.Fecha.DayOfWeek && b.SedeId == query.SedeId)
                .ToList();
            if (bloques.Count == 0)
                continue;

            var ocupados = await OcupadosAsync(profesional.Id, query.Fecha, ct);
            foreach (var bloque in bloques)
            {
                var tipo = tipos.FirstOrDefault(t => t.Id == bloque.TipoTurnoId);
                if (tipo is null)
                    continue;
                var huecos = AgendaRules.GenerarHuecos(query.Fecha, bloque.HoraDesde, bloque.HoraHasta, tipo.DuracionMinutos, zonaHoraria.Zona, ocupados);
                foreach (var hueco in huecos)
                {
                    resultado.Add(new HuecoDto(
                        profesional.Id,
                        $"{profesional.Nombre} {profesional.Apellido}".Trim(),
                        bloque.SedeId,
                        tipo.Id,
                        tipo.Nombre,
                        hueco.Inicio,
                        hueco.Fin));
                    ocupados.Add(hueco);
                }
            }
        }

        return resultado.OrderBy(h => h.Inicio).ThenBy(h => h.Profesional).ToList();
    }

    public async Task<TurnoDto> ReservarAsync(ReservarTurnoCommand command, CancellationToken ct)
    {
        var result = await reservaValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var paciente = await ResolverPacienteAsync(command.PacienteId, ct);
        var huecos = await DisponibilidadAsync(new DisponibilidadQuery(command.SedeId, Guid.Empty, command.ProfesionalId, DateOnly.FromDateTime(command.Inicio.DateTime)), ct);
        var hueco = huecos.FirstOrDefault(h => h.ProfesionalId == command.ProfesionalId && h.TipoTurnoId == command.TipoTurnoId && h.Inicio.UtcDateTime == command.Inicio.UtcDateTime)
            ?? throw new ConflictoException("Ese horario ya no está disponible.");

        var turno = await turnos.ReservarAsync(new Turno
        {
            Id = Guid.NewGuid(),
            PacienteId = paciente.Id,
            ProfesionalId = command.ProfesionalId,
            SedeId = command.SedeId,
            TipoTurnoId = command.TipoTurnoId,
            Inicio = hueco.Inicio,
            Fin = hueco.Fin,
            Estado = EstadoTurno.Reservado,
            MotivoConsulta = command.MotivoConsulta?.Trim()
        }, ct);
        await auditoria.RegistrarAsync(actual.Id, "reserva", "Turno", turno.Id.ToString(), turno.Inicio.ToString("O"), ct);
        return await MapTurnoAsync(turno, ct);
    }

    public async Task<IReadOnlyList<TurnoDto>> DelDiaAsync(DateOnly fecha, Guid? profesionalId, CancellationToken ct)
    {
        if (actual.Rol == RolTenant.Medico.ToString())
            profesionalId = actual.Id;
        var lista = await turnos.DelDiaAsync(fecha, profesionalId, zonaHoraria.Zona, ct);
        var dtos = new List<TurnoDto>();
        foreach (var turno in lista.OrderBy(t => t.Inicio))
            dtos.Add(await MapTurnoAsync(turno, ct));
        return dtos;
    }

    public async Task<IReadOnlyList<TurnoDto>> MiosAsync(CancellationToken ct)
    {
        var paciente = await pacientes.PorUsuarioAsync(actual.Id ?? Guid.Empty, ct)
            ?? throw new NoEncontradoException("No hay un paciente asociado a esta cuenta.");
        var lista = await turnos.DePacienteAsync(paciente.Id, ct);
        var dtos = new List<TurnoDto>();
        foreach (var turno in lista.OrderByDescending(t => t.Inicio))
            dtos.Add(await MapTurnoAsync(turno, ct));
        return dtos;
    }

    public async Task<TurnoDto> CancelarAsync(Guid id, CancellationToken ct)
    {
        var turno = await turnos.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Turno no encontrado.");
        if (actual.Rol == RolTenant.Paciente.ToString())
        {
            var paciente = await pacientes.PorUsuarioAsync(actual.Id ?? Guid.Empty, ct)
                ?? throw new NoEncontradoException("No hay un paciente asociado a esta cuenta.");
            if (turno.PacienteId != paciente.Id)
                throw new ReglaNegocioException("No podés cancelar un turno de otra persona.");
            if (!AgendaRules.PuedeCancelarPaciente(turno.Estado, turno.Inicio, clock.GetUtcNow()))
                throw new ReglaNegocioException("El paciente solo puede cancelar un turno reservado con al menos 24 horas de anticipación.");
        }
        else if (turno.Estado is not (EstadoTurno.Reservado or EstadoTurno.Admitido))
        {
            throw new ReglaNegocioException("Ese turno ya no se puede cancelar.");
        }

        turno.Estado = EstadoTurno.Cancelado;
        await turnos.GuardarAsync(ct);
        await auditoria.RegistrarAsync(actual.Id, "cancelacion", "Turno", turno.Id.ToString(), null, ct);
        return await MapTurnoAsync(turno, ct);
    }

    public async Task<TurnoDto> AdmitirAsync(Guid id, CancellationToken ct)
    {
        var turno = await turnos.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Turno no encontrado.");
        if (turno.Estado != EstadoTurno.Reservado)
            throw new ReglaNegocioException("Solo se admite un turno reservado.");
        turno.Estado = EstadoTurno.Admitido;
        await turnos.GuardarAsync(ct);
        await auditoria.RegistrarAsync(actual.Id, "admision", "Turno", turno.Id.ToString(), null, ct);
        return await MapTurnoAsync(turno, ct);
    }

    public async Task<TurnoDto> ReprogramarAsync(Guid id, ReprogramarTurnoCommand command, CancellationToken ct)
    {
        var turno = await turnos.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Turno no encontrado.");
        if (!AgendaRules.PuedeReprogramar(turno.Estado))
            throw new ReglaNegocioException("Ese turno no se puede reprogramar.");
        var fecha = DateOnly.FromDateTime(command.Inicio.DateTime);
        var huecos = await DisponibilidadAsync(new DisponibilidadQuery(turno.SedeId, Guid.Empty, turno.ProfesionalId, fecha), ct);
        var hueco = huecos.FirstOrDefault(h => h.Inicio.UtcDateTime == command.Inicio.UtcDateTime && h.TipoTurnoId == turno.TipoTurnoId)
            ?? throw new ConflictoException("Ese horario ya no está disponible.");
        turno.Inicio = hueco.Inicio;
        turno.Fin = hueco.Fin;
        await turnos.GuardarAsync(ct);
        await auditoria.RegistrarAsync(actual.Id, "reprogramacion", "Turno", turno.Id.ToString(), turno.Inicio.ToString("O"), ct);
        return await MapTurnoAsync(turno, ct);
    }

    public async Task<ListaEsperaDto> AnotarEsperaAsync(CrearListaEsperaCommand command, CancellationToken ct)
    {
        var paciente = await ResolverPacienteAsync(command.PacienteId, ct);
        var entrada = await listaEspera.AgregarAsync(new ListaEspera
        {
            Id = Guid.NewGuid(),
            PacienteId = paciente.Id,
            ProfesionalId = command.ProfesionalId,
            SedeId = command.SedeId,
            EspecialidadId = command.EspecialidadId,
            Estado = EstadoListaEspera.Pendiente,
            CreadoEn = clock.GetUtcNow(),
            Notas = command.Notas?.Trim()
        }, ct);
        return await MapEsperaAsync(entrada, ct);
    }

    public async Task<IReadOnlyList<ListaEsperaDto>> ListaEsperaAsync(CancellationToken ct)
    {
        var lista = await listaEspera.PendientesAsync(ct);
        var dtos = new List<ListaEsperaDto>();
        foreach (var entrada in lista)
            dtos.Add(await MapEsperaAsync(entrada, ct));
        return dtos;
    }

    public async Task<TurnoDto> AsignarEsperaAsync(Guid id, AsignarListaEsperaCommand command, CancellationToken ct)
    {
        var entrada = await listaEspera.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("La entrada de lista de espera no existe.");
        if (entrada.Estado is EstadoListaEspera.Aceptado or EstadoListaEspera.Cancelado)
            throw new ReglaNegocioException("Esa espera ya fue resuelta.");
        if (entrada.ProfesionalId is null)
            throw new ReglaNegocioException("Elegí un profesional antes de asignar el turno.");

        var turno = await ReservarAsync(new ReservarTurnoCommand(
            entrada.PacienteId,
            entrada.ProfesionalId.Value,
            entrada.SedeId,
            command.TipoTurnoId,
            command.Inicio,
            entrada.Notas), ct);
        entrada.Estado = EstadoListaEspera.Aceptado;
        await listaEspera.GuardarAsync(ct);
        return turno;
    }

    private async Task<List<Intervalo>> OcupadosAsync(Guid profesionalId, DateOnly fecha, CancellationToken ct)
    {
        var turnosDia = await agendas.TurnosDelDiaAsync(profesionalId, fecha, zonaHoraria.Zona, ct);
        var bloqueos = await agendas.BloqueosAsync(profesionalId, ct);
        var inicioDia = AgendaRules.Combinar(fecha, TimeOnly.MinValue, zonaHoraria.Zona);
        var finDia = inicioDia.AddDays(1);
        return turnosDia
            .Where(t => AgendaRules.CuentaComoOcupado(t.Estado))
            .Select(t => new Intervalo(t.Inicio, t.Fin))
            .Concat(bloqueos.Where(b => b.Inicio < finDia && b.Fin > inicioDia).Select(b => new Intervalo(b.Inicio, b.Fin)))
            .ToList();
    }

    private async Task<Paciente> ResolverPacienteAsync(Guid? pacienteId, CancellationToken ct)
    {
        if (actual.Rol == RolTenant.Paciente.ToString())
        {
            return await pacientes.PorUsuarioAsync(actual.Id ?? Guid.Empty, ct)
                ?? throw new NoEncontradoException("No hay un paciente asociado a esta cuenta.");
        }

        if (pacienteId is null || pacienteId == Guid.Empty)
            throw new ReglaNegocioException("Indicá el paciente.");
        return await pacientes.ObtenerAsync(pacienteId.Value, ct) ?? throw new NoEncontradoException("Paciente no encontrado.");
    }

    private async Task<TurnoDto> MapTurnoAsync(Turno turno, CancellationToken ct)
    {
        var paciente = await pacientes.ObtenerAsync(turno.PacienteId, ct);
        var usuarioPaciente = paciente is null ? null : await usuarios.FindByIdAsync(paciente.UsuarioId, ct);
        var profesional = await usuarios.FindByIdAsync(turno.ProfesionalId, ct);
        var sede = await organizacion.ObtenerSedeAsync(turno.SedeId, ct);
        var tipo = await organizacion.ObtenerTipoTurnoAsync(turno.TipoTurnoId, ct);
        return new TurnoDto(
            turno.Id,
            turno.PacienteId,
            usuarioPaciente is null ? "Paciente" : $"{usuarioPaciente.Nombre} {usuarioPaciente.Apellido}".Trim(),
            turno.ProfesionalId,
            profesional is null ? "Profesional" : $"{profesional.Nombre} {profesional.Apellido}".Trim(),
            turno.SedeId,
            sede?.Nombre ?? "",
            turno.TipoTurnoId,
            tipo?.Nombre ?? "",
            turno.Inicio,
            turno.Fin,
            turno.Estado,
            turno.MotivoConsulta);
    }

    private async Task<ListaEsperaDto> MapEsperaAsync(ListaEspera entrada, CancellationToken ct)
    {
        var paciente = await pacientes.ObtenerAsync(entrada.PacienteId, ct);
        var usuario = paciente is null ? null : await usuarios.FindByIdAsync(paciente.UsuarioId, ct);
        return new ListaEsperaDto(
            entrada.Id,
            entrada.PacienteId,
            usuario is null ? "Paciente" : $"{usuario.Nombre} {usuario.Apellido}".Trim(),
            entrada.ProfesionalId,
            entrada.SedeId,
            entrada.EspecialidadId,
            entrada.Estado,
            entrada.CreadoEn,
            entrada.Notas);
    }
}
