using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class HistoriasMedicasService(
    IHistoriaMedicaStore historias,
    IPacienteStore pacientes,
    ITenantUserStore usuarios,
    IUsuarioActual actual,
    IAuditoriaStore auditoria,
    IValidator<GuardarHistoriaMedicaCommand> validator,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<HistoriaMedicaDto>> ListarAsync(CancellationToken ct)
    {
        var lista = await historias.PacientesAsync(ct);
        var cuentas = (await usuarios.ListarPorRolAsync(RolTenant.Paciente, null, ct)).ToDictionary(u => u.Id);
        var fichas = (await historias.ListarAsync(ct)).ToDictionary(h => h.PacienteId);
        return lista
            .Select(p => Map(p, cuentas.GetValueOrDefault(p.UsuarioId), fichas.GetValueOrDefault(p.Id)))
            .OrderBy(h => h.Apellido)
            .ThenBy(h => h.Nombre)
            .ToList();
    }

    public async Task<HistoriaMedicaDto> ObtenerAsync(Guid pacienteId, CancellationToken ct)
    {
        var paciente = await pacientes.ObtenerAsync(pacienteId, ct) ?? throw new NoEncontradoException("Paciente no encontrado.");
        var usuario = await usuarios.FindByIdAsync(paciente.UsuarioId, ct);
        return Map(paciente, usuario, await historias.PorPacienteAsync(pacienteId, ct));
    }

    public async Task<HistoriaMedicaDto> GuardarAsync(Guid pacienteId, GuardarHistoriaMedicaCommand command, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var paciente = await pacientes.ObtenerAsync(pacienteId, ct) ?? throw new NoEncontradoException("Paciente no encontrado.");
        paciente.Documento = command.Documento.Trim();
        paciente.FechaNacimiento = command.FechaNacimiento;
        paciente.Telefono = command.Telefono.Trim();

        var historia = await historias.PorPacienteAsync(pacienteId, ct);
        if (historia is null)
        {
            historia = new HistoriaMedica { Id = Guid.NewGuid(), PacienteId = pacienteId };
            historias.Agregar(historia);
        }
        historia.GrupoSanguineo = Limpiar(command.GrupoSanguineo);
        historia.Alergias = Limpiar(command.Alergias);
        historia.AntecedentesPersonales = Limpiar(command.AntecedentesPersonales);
        historia.AntecedentesFamiliares = Limpiar(command.AntecedentesFamiliares);
        historia.MedicacionHabitual = Limpiar(command.MedicacionHabitual);
        historia.Habitos = Limpiar(command.Habitos);
        historia.ObraSocial = Limpiar(command.ObraSocial);
        historia.NumeroAfiliado = Limpiar(command.NumeroAfiliado);
        historia.ContactoEmergencia = Limpiar(command.ContactoEmergencia);
        historia.TelefonoEmergencia = Limpiar(command.TelefonoEmergencia);
        historia.Observaciones = Limpiar(command.Observaciones);
        historia.ActualizadoEn = clock.GetUtcNow();
        historia.ActualizadoPor = actual.Id;
        await historias.GuardarAsync(ct);

        await usuarios.ActualizarNombreAsync(paciente.UsuarioId, command.Nombre.Trim(), command.Apellido.Trim(), ct);
        await auditoria.RegistrarAsync(actual.Id, "historia", "HistoriaMedica", historia.Id.ToString(), paciente.Id.ToString(), ct);
        return await ObtenerAsync(pacienteId, ct);
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static HistoriaMedicaDto Map(Paciente paciente, UsuarioTenant? usuario, HistoriaMedica? historia) => new(
        paciente.Id,
        usuario?.Nombre ?? "",
        usuario?.Apellido ?? "",
        usuario?.Email ?? "",
        paciente.Documento,
        paciente.FechaNacimiento,
        paciente.Telefono,
        historia?.GrupoSanguineo,
        historia?.Alergias,
        historia?.AntecedentesPersonales,
        historia?.AntecedentesFamiliares,
        historia?.MedicacionHabitual,
        historia?.Habitos,
        historia?.ObraSocial,
        historia?.NumeroAfiliado,
        historia?.ContactoEmergencia,
        historia?.TelefonoEmergencia,
        historia?.Observaciones,
        historia?.ActualizadoEn);
}
