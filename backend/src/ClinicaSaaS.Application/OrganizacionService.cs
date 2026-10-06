using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class OrganizacionService(IOrganizacionStore store, ITenantUserStore usuarios, IAuditoriaStore auditoria, IUsuarioActual actual)
{
    public async Task<IReadOnlyList<SedeDto>> SedesAsync(CancellationToken ct) =>
        (await store.SedesAsync(ct)).Select(s => new SedeDto(s.Id, s.Nombre, s.Direccion, s.Activa)).ToList();

    public async Task<SedeDto> CrearSedeAsync(GuardarSedeCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
            throw new ReglaNegocioException("La sede necesita un nombre.");
        var sede = await store.AgregarSedeAsync(new Sede
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre.Trim(),
            Direccion = command.Direccion.Trim(),
            Activa = true
        }, ct);
        await Auditar("alta", "Sede", sede.Id, sede.Nombre, ct);
        return new SedeDto(sede.Id, sede.Nombre, sede.Direccion, sede.Activa);
    }

    public async Task<SedeDto> ActualizarSedeAsync(Guid id, GuardarSedeCommand command, CancellationToken ct)
    {
        var sede = await store.ObtenerSedeAsync(id, ct) ?? throw new NoEncontradoException("Sede no encontrada.");
        sede.Nombre = command.Nombre.Trim();
        sede.Direccion = command.Direccion.Trim();
        await store.GuardarAsync(ct);
        await Auditar("edicion", "Sede", sede.Id, sede.Nombre, ct);
        return new SedeDto(sede.Id, sede.Nombre, sede.Direccion, sede.Activa);
    }

    public async Task<IReadOnlyList<ServicioDto>> ServiciosAsync(CancellationToken ct) =>
        (await store.ServiciosAsync(ct)).Select(s => new ServicioDto(s.Id, s.SedeId, s.Nombre)).ToList();

    public async Task<ServicioDto> CrearServicioAsync(GuardarServicioCommand command, CancellationToken ct)
    {
        _ = await store.ObtenerSedeAsync(command.SedeId, ct) ?? throw new NoEncontradoException("Sede no encontrada.");
        var servicio = await store.AgregarServicioAsync(new Servicio
        {
            Id = Guid.NewGuid(),
            SedeId = command.SedeId,
            Nombre = command.Nombre.Trim()
        }, ct);
        await Auditar("alta", "Servicio", servicio.Id, servicio.Nombre, ct);
        return new ServicioDto(servicio.Id, servicio.SedeId, servicio.Nombre);
    }

    public async Task<ServicioDto> ActualizarServicioAsync(Guid id, GuardarServicioCommand command, CancellationToken ct)
    {
        var servicio = await store.ObtenerServicioAsync(id, ct) ?? throw new NoEncontradoException("Servicio no encontrado.");
        servicio.Nombre = command.Nombre.Trim();
        servicio.SedeId = command.SedeId;
        await store.GuardarAsync(ct);
        return new ServicioDto(servicio.Id, servicio.SedeId, servicio.Nombre);
    }

    public async Task<IReadOnlyList<EspecialidadDto>> EspecialidadesAsync(CancellationToken ct) =>
        (await store.EspecialidadesAsync(ct)).Select(e => new EspecialidadDto(e.Id, e.Nombre)).ToList();

    public async Task<EspecialidadDto> CrearEspecialidadAsync(GuardarEspecialidadCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
            throw new ReglaNegocioException("La especialidad necesita un nombre.");
        var item = await store.AgregarEspecialidadAsync(new Especialidad { Id = Guid.NewGuid(), Nombre = command.Nombre.Trim() }, ct);
        await Auditar("alta", "Especialidad", item.Id, item.Nombre, ct);
        return new EspecialidadDto(item.Id, item.Nombre);
    }

    public async Task<EspecialidadDto> ActualizarEspecialidadAsync(Guid id, GuardarEspecialidadCommand command, CancellationToken ct)
    {
        var item = await store.ObtenerEspecialidadAsync(id, ct) ?? throw new NoEncontradoException("Especialidad no encontrada.");
        item.Nombre = command.Nombre.Trim();
        await store.GuardarAsync(ct);
        return new EspecialidadDto(item.Id, item.Nombre);
    }

    public async Task<IReadOnlyList<TipoTurnoDto>> TiposTurnoAsync(CancellationToken ct) =>
        (await store.TiposTurnoAsync(ct)).Select(MapTipo).ToList();

    public async Task<TipoTurnoDto> CrearTipoTurnoAsync(GuardarTipoTurnoCommand command, CancellationToken ct)
    {
        ValidarTipo(command);
        var tipo = await store.AgregarTipoTurnoAsync(new TipoTurno
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre.Trim(),
            DuracionMinutos = command.DuracionMinutos,
            EspecialidadId = command.EspecialidadId
        }, ct);
        await Auditar("alta", "TipoTurno", tipo.Id, tipo.Nombre, ct);
        return MapTipo(tipo);
    }

    public async Task<TipoTurnoDto> ActualizarTipoTurnoAsync(Guid id, GuardarTipoTurnoCommand command, CancellationToken ct)
    {
        ValidarTipo(command);
        var tipo = await store.ObtenerTipoTurnoAsync(id, ct) ?? throw new NoEncontradoException("Tipo de turno no encontrado.");
        tipo.Nombre = command.Nombre.Trim();
        tipo.DuracionMinutos = command.DuracionMinutos;
        tipo.EspecialidadId = command.EspecialidadId;
        await store.GuardarAsync(ct);
        return MapTipo(tipo);
    }

    public async Task<IReadOnlyList<ProfesionalDto>> ProfesionalesAsync(Guid? especialidadId, CancellationToken ct)
    {
        var lista = await usuarios.ListarPorRolAsync(RolTenant.Medico, especialidadId, ct);
        return lista.Select(p => new ProfesionalDto(p.Id, p.Nombre, p.Apellido, p.Email, p.Matricula, p.EspecialidadId)).ToList();
    }

    private async Task Auditar(string accion, string entidad, Guid id, string? detalle, CancellationToken ct) =>
        await auditoria.RegistrarAsync(actual.Id, accion, entidad, id.ToString(), detalle, ct);

    private static void ValidarTipo(GuardarTipoTurnoCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
            throw new ReglaNegocioException("El tipo de turno necesita un nombre.");
        if (command.DuracionMinutos is < 5 or > 240)
            throw new ReglaNegocioException("La duración debe estar entre 5 y 240 minutos.");
    }

    private static TipoTurnoDto MapTipo(TipoTurno tipo) =>
        new(tipo.Id, tipo.Nombre, tipo.DuracionMinutos, tipo.EspecialidadId);
}

public class EmpleadosService(ITenantUserStore usuarios, IOrganizacionStore organizacion, IAuditoriaStore auditoria, IUsuarioActual actual)
{
    public async Task<ImportacionEmpleadosDto> ImportarAsync(ImportarEmpleadosCommand command, CancellationToken ct)
    {
        var lineas = command.Csv.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lineas.Length < 2)
            throw new ReglaNegocioException("El CSV debe incluir encabezado y al menos una fila.");

        var creados = new List<EmpleadoCreadoDto>();
        var rechazados = new List<FilaRechazadaDto>();
        for (var i = 1; i < lineas.Length; i++)
        {
            var fila = i + 1;
            var cols = lineas[i].Split(',');
            if (cols.Length < 4)
            {
                rechazados.Add(new FilaRechazadaDto(fila, "Faltan columnas."));
                continue;
            }

            var nombre = cols[0].Trim();
            var apellido = cols[1].Trim();
            var email = cols[2].Trim();
            var rolTexto = cols[3].Trim();
            var matricula = cols.Length > 4 ? cols[4].Trim() : null;
            var especialidadNombre = cols.Length > 5 ? cols[5].Trim() : null;
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) || string.IsNullOrWhiteSpace(email))
            {
                rechazados.Add(new FilaRechazadaDto(fila, "Nombre, apellido y email son obligatorios."));
                continue;
            }

            if (!Enum.TryParse<RolTenant>(rolTexto, true, out var rol) || rol == RolTenant.Paciente)
            {
                rechazados.Add(new FilaRechazadaDto(fila, "El rol debe ser Medico, Secretario o AdminTenant."));
                continue;
            }

            if (await usuarios.FindByEmailAsync(email, ct) is not null)
            {
                rechazados.Add(new FilaRechazadaDto(fila, "El email ya existe."));
                continue;
            }

            Guid? especialidadId = null;
            if (rol == RolTenant.Medico)
            {
                if (string.IsNullOrWhiteSpace(especialidadNombre))
                {
                    rechazados.Add(new FilaRechazadaDto(fila, "El médico necesita una especialidad."));
                    continue;
                }

                var especialidad = await organizacion.ObtenerEspecialidadPorNombreAsync(especialidadNombre, ct);
                if (especialidad is null)
                {
                    rechazados.Add(new FilaRechazadaDto(fila, "Especialidad inexistente."));
                    continue;
                }

                especialidadId = especialidad.Id;
            }

            var clave = ClaveTemporal.Generar();
            var creado = await usuarios.CreateAsync(email, clave, nombre, apellido, rol, string.IsNullOrWhiteSpace(matricula) ? null : matricula, especialidadId, true, ct);
            creados.Add(new EmpleadoCreadoDto(fila, creado.Email, clave, rol));
        }

        await auditoria.RegistrarAsync(actual.Id, "importacion", "Empleado", null, $"{creados.Count} creados, {rechazados.Count} rechazados", ct);
        return new ImportacionEmpleadosDto(creados, rechazados);
    }
}

public static class ClaveTemporal
{
    public static string Generar()
    {
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alfabeto[bytes[i] % alfabeto.Length];
        return $"Tmp{new string(chars)}1a";
    }
}
