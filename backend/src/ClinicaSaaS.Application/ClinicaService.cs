using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class ClinicaService(
    IClinicaStore clinica,
    ITurnoStore turnos,
    IPacienteStore pacientes,
    ITenantUserStore usuarios,
    IFacturacionStore facturacion,
    IUsuarioActual actual,
    IAuditoriaStore auditoria,
    IValidator<GuardarEncuentroCommand> encuentroValidator,
    IValidator<CrearRecetaCommand> recetaValidator,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<DiagnosticoDto>> DiagnosticosAsync(CancellationToken ct) =>
        (await clinica.DiagnosticosAsync(ct)).Select(d => new DiagnosticoDto(d.Id, d.Codigo, d.Nombre)).ToList();

    public async Task<EncuentroDto> GuardarEncuentroAsync(GuardarEncuentroCommand command, CancellationToken ct)
    {
        var result = await encuentroValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var turno = await turnos.ObtenerAsync(command.TurnoId, ct) ?? throw new NoEncontradoException("Turno no encontrado.");
        if (actual.Rol == RolTenant.Medico.ToString() && turno.ProfesionalId != actual.Id)
            throw new ReglaNegocioException("Solo el profesional del turno puede cargar el encuentro.");
        if (turno.Estado is EstadoTurno.Cancelado or EstadoTurno.Ausente or EstadoTurno.Completado)
            throw new ReglaNegocioException("Ese turno no admite un encuentro.");

        var encuentro = await clinica.EncuentroPorTurnoAsync(turno.Id, ct);
        if (encuentro is null)
        {
            encuentro = new Encuentro
            {
                Id = Guid.NewGuid(),
                TurnoId = turno.Id,
                PacienteId = turno.PacienteId,
                ProfesionalId = turno.ProfesionalId,
                CreadoEn = clock.GetUtcNow()
            };
            encuentro = await clinica.AgregarEncuentroAsync(encuentro, ct);
        }
        else if (encuentro.Cerrado)
        {
            throw new ReglaNegocioException("El encuentro ya está cerrado.");
        }

        encuentro.Nota = command.Nota?.Trim();
        encuentro.TensionArterial = command.TensionArterial?.Trim();
        encuentro.FrecuenciaCardiaca = command.FrecuenciaCardiaca;
        encuentro.Temperatura = command.Temperatura;
        encuentro.PesoKg = command.PesoKg;
        await clinica.ReemplazarDiagnosticosAsync(encuentro.Id, command.DiagnosticoIds, ct);
        if (turno.Estado is EstadoTurno.Reservado or EstadoTurno.Admitido)
            turno.Estado = EstadoTurno.EnCurso;
        await turnos.GuardarAsync(ct);
        await clinica.GuardarAsync(ct);
        await auditoria.RegistrarAsync(actual.Id, "encuentro", "Encuentro", encuentro.Id.ToString(), null, ct);
        return await MapEncuentroAsync(encuentro, ct);
    }

    public async Task<EncuentroDto> CerrarEncuentroAsync(Guid id, CancellationToken ct)
    {
        var encuentro = await clinica.ObtenerEncuentroAsync(id, ct) ?? throw new NoEncontradoException("Encuentro no encontrado.");
        if (!EncuentroRules.PuedeCerrar(encuentro.Nota))
            throw new ReglaNegocioException("Para cerrar el encuentro hace falta una nota clínica.");
        if (encuentro.Cerrado)
            return await MapEncuentroAsync(encuentro, ct);

        var turno = await turnos.ObtenerAsync(encuentro.TurnoId, ct) ?? throw new NoEncontradoException("Turno no encontrado.");
        encuentro.Cerrado = true;
        turno.Estado = EstadoTurno.Completado;
        await clinica.GuardarAsync(ct);
        await turnos.GuardarAsync(ct);
        await CrearComprobanteSiFaltaAsync(turno, ct);
        await auditoria.RegistrarAsync(actual.Id, "cierre", "Encuentro", encuentro.Id.ToString(), null, ct);
        return await MapEncuentroAsync(encuentro, ct);
    }

    public async Task<RecetaDto> CrearRecetaAsync(CrearRecetaCommand command, CancellationToken ct)
    {
        var result = await recetaValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        var encuentro = await clinica.ObtenerEncuentroAsync(command.EncuentroId, ct) ?? throw new NoEncontradoException("Encuentro no encontrado.");
        if (actual.Rol == RolTenant.Medico.ToString() && encuentro.ProfesionalId != actual.Id)
            throw new ReglaNegocioException("Solo el profesional del encuentro puede emitir la receta.");

        var receta = await clinica.AgregarRecetaAsync(new Receta
        {
            Id = Guid.NewGuid(),
            EncuentroId = encuentro.Id,
            PacienteId = encuentro.PacienteId,
            ProfesionalId = encuentro.ProfesionalId,
            Indicaciones = command.Indicaciones?.Trim(),
            CreadoEn = clock.GetUtcNow(),
            Items = command.Items.Select(i => new RecetaItem
            {
                Id = Guid.NewGuid(),
                Medicamento = i.Medicamento.Trim(),
                Dosis = i.Dosis.Trim(),
                Frecuencia = i.Frecuencia.Trim(),
                Duracion = i.Duracion.Trim()
            }).ToList()
        }, ct);
        await auditoria.RegistrarAsync(actual.Id, "receta", "Receta", receta.Id.ToString(), null, ct);
        return await MapRecetaAsync(receta, ct);
    }

    public async Task<RecetaDto> ObtenerRecetaAsync(Guid id, CancellationToken ct)
    {
        var receta = await clinica.ObtenerRecetaAsync(id, ct) ?? throw new NoEncontradoException("Receta no encontrada.");
        await AsegurarAccesoPacienteAsync(receta.PacienteId, ct);
        return await MapRecetaAsync(receta, ct);
    }

    public async Task<HistoriaDto> HistoriaAsync(Guid? pacienteId, CancellationToken ct)
    {
        Paciente paciente;
        if (actual.Rol == RolTenant.Paciente.ToString())
        {
            paciente = await pacientes.PorUsuarioAsync(actual.Id ?? Guid.Empty, ct)
                ?? throw new NoEncontradoException("No hay un paciente asociado a esta cuenta.");
        }
        else
        {
            if (pacienteId is null)
                throw new ReglaNegocioException("Indicá el paciente.");
            paciente = await pacientes.ObtenerAsync(pacienteId.Value, ct) ?? throw new NoEncontradoException("Paciente no encontrado.");
        }

        var usuario = await usuarios.FindByIdAsync(paciente.UsuarioId, ct);
        var encuentros = await clinica.EncuentrosDePacienteAsync(paciente.Id, ct);
        var recetas = await clinica.RecetasDePacienteAsync(paciente.Id, ct);
        var encuentrosDto = new List<EncuentroDto>();
        foreach (var encuentro in encuentros.OrderByDescending(e => e.CreadoEn))
            encuentrosDto.Add(await MapEncuentroAsync(encuentro, ct));
        var recetasDto = new List<RecetaDto>();
        foreach (var receta in recetas.OrderByDescending(r => r.CreadoEn))
            recetasDto.Add(await MapRecetaAsync(receta, ct));

        return new HistoriaDto(
            new PacienteResumenDto(
                paciente.Id,
                usuario is null ? "Paciente" : $"{usuario.Nombre} {usuario.Apellido}".Trim(),
                paciente.Documento,
                paciente.FechaNacimiento,
                paciente.Telefono),
            encuentrosDto,
            recetasDto);
    }

    public async Task<IReadOnlyList<RecetaDto>> RecetasMiasAsync(CancellationToken ct)
    {
        var historia = await HistoriaAsync(null, ct);
        return historia.Recetas;
    }

    private async Task CrearComprobanteSiFaltaAsync(Turno turno, CancellationToken ct)
    {
        if (await facturacion.ComprobantePorTurnoAsync(turno.Id, ct) is not null)
            return;
        var hoy = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var arancel = await facturacion.ArancelVigenteAsync(turno.TipoTurnoId, hoy, ct);
        var monto = arancel?.Monto ?? 0;
        var tipoNombre = arancel is null ? "Consulta" : "Consulta";
        await facturacion.AgregarComprobanteAsync(new Comprobante
        {
            Id = Guid.NewGuid(),
            TurnoId = turno.Id,
            PacienteId = turno.PacienteId,
            Total = FacturacionRules.Total([monto]),
            Estado = EstadoComprobante.Pendiente,
            CreadoEn = clock.GetUtcNow(),
            Items =
            [
                new ComprobanteItem
                {
                    Id = Guid.NewGuid(),
                    Descripcion = tipoNombre,
                    Importe = monto
                }
            ]
        }, ct);
    }

    private async Task AsegurarAccesoPacienteAsync(Guid pacienteId, CancellationToken ct)
    {
        if (actual.Rol != RolTenant.Paciente.ToString())
            return;
        var paciente = await pacientes.PorUsuarioAsync(actual.Id ?? Guid.Empty, ct)
            ?? throw new NoEncontradoException("No hay un paciente asociado a esta cuenta.");
        if (paciente.Id != pacienteId)
            throw new ReglaNegocioException("No podés ver datos de otra persona.");
    }

    private async Task<EncuentroDto> MapEncuentroAsync(Encuentro encuentro, CancellationToken ct)
    {
        var diagnosticos = await clinica.DiagnosticosDeEncuentroAsync(encuentro.Id, ct);
        return new EncuentroDto(
            encuentro.Id,
            encuentro.TurnoId,
            encuentro.PacienteId,
            encuentro.ProfesionalId,
            encuentro.Nota,
            encuentro.TensionArterial,
            encuentro.FrecuenciaCardiaca,
            encuentro.Temperatura,
            encuentro.PesoKg,
            encuentro.Cerrado,
            encuentro.CreadoEn,
            diagnosticos.Select(d => new DiagnosticoDto(d.Id, d.Codigo, d.Nombre)).ToList());
    }

    private async Task<RecetaDto> MapRecetaAsync(Receta receta, CancellationToken ct)
    {
        var profesional = await usuarios.FindByIdAsync(receta.ProfesionalId, ct);
        var items = receta.Items.Count > 0 ? receta.Items : (await clinica.ItemsRecetaAsync(receta.Id, ct)).ToList();
        return new RecetaDto(
            receta.Id,
            receta.EncuentroId,
            receta.PacienteId,
            receta.ProfesionalId,
            profesional is null ? "Profesional" : $"{profesional.Nombre} {profesional.Apellido}".Trim(),
            receta.Indicaciones,
            receta.CreadoEn,
            items.Select(i => new RecetaItemDto(i.Medicamento, i.Dosis, i.Frecuencia, i.Duracion)).ToList());
    }
}

public class FacturacionService(
    IFacturacionStore store,
    IOrganizacionStore organizacion,
    IPacienteStore pacientes,
    ITenantUserStore usuarios,
    IUsuarioActual actual,
    IAuditoriaStore auditoria,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<ArancelDto>> ArancelesAsync(CancellationToken ct)
    {
        var tipos = await organizacion.TiposTurnoAsync(ct);
        return (await store.ArancelesAsync(ct))
            .OrderByDescending(a => a.VigenteDesde)
            .Select(a => new ArancelDto(a.Id, a.TipoTurnoId, tipos.FirstOrDefault(t => t.Id == a.TipoTurnoId)?.Nombre ?? "", a.Monto, a.VigenteDesde))
            .ToList();
    }

    public async Task<ArancelDto> CrearArancelAsync(CrearArancelCommand command, CancellationToken ct)
    {
        if (command.Monto < 0)
            throw new ReglaNegocioException("El monto no puede ser negativo.");
        var tipo = await organizacion.ObtenerTipoTurnoAsync(command.TipoTurnoId, ct)
            ?? throw new NoEncontradoException("Tipo de turno no encontrado.");
        var arancel = await store.AgregarArancelAsync(new Arancel
        {
            Id = Guid.NewGuid(),
            TipoTurnoId = command.TipoTurnoId,
            Monto = command.Monto,
            VigenteDesde = command.VigenteDesde
        }, ct);
        await auditoria.RegistrarAsync(actual.Id, "arancel", "Arancel", arancel.Id.ToString(), arancel.Monto.ToString("0.00"), ct);
        return new ArancelDto(arancel.Id, arancel.TipoTurnoId, tipo.Nombre, arancel.Monto, arancel.VigenteDesde);
    }

    public async Task<IReadOnlyList<ComprobanteDto>> ComprobantesAsync(CancellationToken ct)
    {
        var lista = await store.ComprobantesAsync(ct);
        var dtos = new List<ComprobanteDto>();
        foreach (var comprobante in lista.OrderByDescending(c => c.CreadoEn))
            dtos.Add(await MapAsync(comprobante, ct));
        return dtos;
    }

    public async Task<ComprobanteDto> PagarAsync(Guid id, PagarComprobanteCommand command, CancellationToken ct)
    {
        var comprobante = await store.ObtenerComprobanteAsync(id, ct) ?? throw new NoEncontradoException("Comprobante no encontrado.");
        if (!FacturacionRules.PuedePagar(comprobante.Estado))
            throw new ReglaNegocioException("Ese comprobante no admite un pago.");
        comprobante.Estado = EstadoComprobante.Pagado;
        comprobante.MetodoPago = command.Metodo;
        comprobante.PagadoEn = clock.GetUtcNow();
        comprobante.Total = FacturacionRules.Total((await store.ItemsAsync(comprobante.Id, ct)).Select(i => i.Importe));
        await store.GuardarAsync(ct);
        await auditoria.RegistrarAsync(actual.Id, "pago", "Comprobante", comprobante.Id.ToString(), command.Metodo.ToString(), ct);
        return await MapAsync(comprobante, ct);
    }

    private async Task<ComprobanteDto> MapAsync(Comprobante comprobante, CancellationToken ct)
    {
        var paciente = await pacientes.ObtenerAsync(comprobante.PacienteId, ct);
        var usuario = paciente is null ? null : await usuarios.FindByIdAsync(paciente.UsuarioId, ct);
        var items = comprobante.Items.Count > 0
            ? comprobante.Items
            : (await store.ItemsAsync(comprobante.Id, ct)).ToList();
        return new ComprobanteDto(
            comprobante.Id,
            comprobante.TurnoId,
            comprobante.PacienteId,
            usuario is null ? "Paciente" : $"{usuario.Nombre} {usuario.Apellido}".Trim(),
            comprobante.Total,
            comprobante.Estado,
            comprobante.MetodoPago,
            comprobante.CreadoEn,
            comprobante.PagadoEn,
            items.Select(i => new ComprobanteItemDto(i.Descripcion, i.Importe)).ToList());
    }
}
