using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure.Persistence;

public class CatalogStore(CatalogDbContext db) : ICatalogStore
{
    public Task<bool> ExisteSlugAsync(string slug, CancellationToken ct) =>
        db.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task AgregarAsync(Tenant tenant, CancellationToken ct)
    {
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Tenant>> ListarAsync(CancellationToken ct) =>
        await db.Tenants.OrderBy(t => t.Nombre).ToListAsync(ct);

    public Task<Tenant?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> ObtenerPorSlugAsync(string slug, CancellationToken ct) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public Task GuardarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class PlatformUserStore(UserManager<PlatformUser> users) : IPlatformUserStore
{
    public async Task<PlatformLogin?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);
        return user is null ? null : new PlatformLogin(user.Id, user.Email ?? email, user.Nombre);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is not null && await users.CheckPasswordAsync(user, password);
    }
}

public class TenantUserStore(UserManager<TenantUser> users) : ITenantUserStore
{
    public async Task<UsuarioTenant?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<UsuarioTenant?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        return user is null ? null : Map(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is not null && await users.CheckPasswordAsync(user, password);
    }

    public async Task<UsuarioTenant> CreateAsync(string email, string password, string nombre, string apellido, RolTenant rol, string? matricula, Guid? especialidadId, bool debeCambiarClave, CancellationToken ct)
    {
        var user = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombre = nombre,
            Apellido = apellido,
            Rol = rol,
            Matricula = matricula,
            EspecialidadId = especialidadId,
            DebeCambiarClave = debeCambiarClave
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new ReglaNegocioException(string.Join(' ', result.Errors.Select(e => e.Description)));
        return Map(user);
    }

    public async Task<IReadOnlyList<UsuarioTenant>> ListarPorRolAsync(RolTenant rol, Guid? especialidadId, CancellationToken ct)
    {
        var query = users.Users.Where(u => u.Rol == rol);
        if (especialidadId is Guid id)
            query = query.Where(u => u.EspecialidadId == id);
        var lista = await query.OrderBy(u => u.Apellido).ToListAsync(ct);
        return lista.Select(Map).ToList();
    }

    private static UsuarioTenant Map(TenantUser user) =>
        new(user.Id, user.Email ?? "", user.Nombre, user.Apellido, user.Rol, user.Matricula, user.EspecialidadId, user.DebeCambiarClave);
}

public class ClinicaStores(TenantDbContext db) :
    IOrganizacionStore,
    IAgendaStore,
    ITurnoStore,
    IPacienteStore,
    IListaEsperaStore,
    IClinicaStore,
    IFacturacionStore,
    IAuditoriaStore
{
    public async Task<IReadOnlyList<Sede>> SedesAsync(CancellationToken ct) =>
        await db.Sedes.OrderBy(s => s.Nombre).ToListAsync(ct);

    public async Task<Sede> AgregarSedeAsync(Sede sede, CancellationToken ct)
    {
        db.Sedes.Add(sede);
        await db.SaveChangesAsync(ct);
        return sede;
    }

    public Task<Sede?> ObtenerSedeAsync(Guid id, CancellationToken ct) => db.Sedes.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Servicio>> ServiciosAsync(CancellationToken ct) =>
        await db.Servicios.OrderBy(s => s.Nombre).ToListAsync(ct);

    public async Task<Servicio> AgregarServicioAsync(Servicio servicio, CancellationToken ct)
    {
        db.Servicios.Add(servicio);
        await db.SaveChangesAsync(ct);
        return servicio;
    }

    public Task<Servicio?> ObtenerServicioAsync(Guid id, CancellationToken ct) => db.Servicios.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Especialidad>> EspecialidadesAsync(CancellationToken ct) =>
        await db.Especialidades.OrderBy(e => e.Nombre).ToListAsync(ct);

    public Task<Especialidad?> ObtenerEspecialidadPorNombreAsync(string nombre, CancellationToken ct) =>
        db.Especialidades.FirstOrDefaultAsync(e => e.Nombre.ToLower() == nombre.ToLower(), ct);

    public async Task<Especialidad> AgregarEspecialidadAsync(Especialidad especialidad, CancellationToken ct)
    {
        db.Especialidades.Add(especialidad);
        await db.SaveChangesAsync(ct);
        return especialidad;
    }

    public Task<Especialidad?> ObtenerEspecialidadAsync(Guid id, CancellationToken ct) =>
        db.Especialidades.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<TipoTurno>> TiposTurnoAsync(CancellationToken ct) =>
        await db.TiposTurno.OrderBy(t => t.Nombre).ToListAsync(ct);

    public Task<TipoTurno?> ObtenerTipoTurnoAsync(Guid id, CancellationToken ct) =>
        db.TiposTurno.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TipoTurno> AgregarTipoTurnoAsync(TipoTurno tipo, CancellationToken ct)
    {
        db.TiposTurno.Add(tipo);
        await db.SaveChangesAsync(ct);
        return tipo;
    }

    public Task GuardarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<AgendaSemanal>> ListarAsync(Guid profesionalId, CancellationToken ct) =>
        await db.Agendas.Where(a => a.ProfesionalId == profesionalId).OrderBy(a => a.Dia).ThenBy(a => a.HoraDesde).ToListAsync(ct);

    public async Task ReemplazarAsync(Guid profesionalId, IReadOnlyList<AgendaSemanal> bloques, CancellationToken ct)
    {
        var actuales = await db.Agendas.Where(a => a.ProfesionalId == profesionalId).ToListAsync(ct);
        db.Agendas.RemoveRange(actuales);
        db.Agendas.AddRange(bloques);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BloqueoAgenda>> BloqueosAsync(Guid profesionalId, CancellationToken ct) =>
        await db.Bloqueos.Where(b => b.ProfesionalId == profesionalId).OrderBy(b => b.Inicio).ToListAsync(ct);

    public async Task<BloqueoAgenda> AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken ct)
    {
        db.Bloqueos.Add(bloqueo);
        await db.SaveChangesAsync(ct);
        return bloqueo;
    }

    public async Task EliminarBloqueoAsync(Guid id, CancellationToken ct)
    {
        var bloqueo = await db.Bloqueos.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NoEncontradoException("Bloqueo no encontrado.");
        db.Bloqueos.Remove(bloqueo);
        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<Turno>> TurnosDelDiaAsync(Guid profesionalId, DateOnly fecha, TimeZoneInfo zona, CancellationToken ct) =>
        DelDiaInternoAsync(fecha, profesionalId, zona, ct);

    public Task<Turno?> ObtenerAsync(Guid id, CancellationToken ct) => db.Turnos.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<IReadOnlyList<Turno>> DelDiaAsync(DateOnly fecha, Guid? profesionalId, TimeZoneInfo zona, CancellationToken ct) =>
        DelDiaInternoAsync(fecha, profesionalId, zona, ct);

    public async Task<IReadOnlyList<Turno>> DePacienteAsync(Guid pacienteId, CancellationToken ct) =>
        await db.Turnos.Where(t => t.PacienteId == pacienteId).ToListAsync(ct);

    public async Task<Turno> ReservarAsync(Turno turno, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var existentes = await db.Turnos
            .Where(t => t.ProfesionalId == turno.ProfesionalId && t.Inicio < turno.Fin && t.Fin > turno.Inicio)
            .ToListAsync(ct);
        var nuevo = new Intervalo(turno.Inicio, turno.Fin);
        if (AgendaRules.HaySolape(existentes.Select(t => (new Intervalo(t.Inicio, t.Fin), t.Estado)), nuevo))
            throw new ConflictoException("El horario ya no está disponible.");
        db.Turnos.Add(turno);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return turno;
    }

    async Task ITurnoStore.GuardarAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);

    public Task<Paciente?> PorUsuarioAsync(Guid usuarioId, CancellationToken ct) =>
        db.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

    Task<Paciente?> IPacienteStore.ObtenerAsync(Guid id, CancellationToken ct) => db.Pacientes.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Paciente> AgregarAsync(Paciente paciente, CancellationToken ct)
    {
        db.Pacientes.Add(paciente);
        await db.SaveChangesAsync(ct);
        return paciente;
    }

    public async Task<ListaEspera> AgregarAsync(ListaEspera entrada, CancellationToken ct)
    {
        db.ListaEspera.Add(entrada);
        await db.SaveChangesAsync(ct);
        return entrada;
    }

    public async Task<IReadOnlyList<ListaEspera>> PendientesAsync(CancellationToken ct) =>
        await db.ListaEspera.Where(l => l.Estado == EstadoListaEspera.Pendiente || l.Estado == EstadoListaEspera.Ofrecido)
            .OrderBy(l => l.CreadoEn).ToListAsync(ct);

    Task<ListaEspera?> IListaEsperaStore.ObtenerAsync(Guid id, CancellationToken ct) => db.ListaEspera.FirstOrDefaultAsync(l => l.Id == id, ct);

    async Task IListaEsperaStore.GuardarAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Diagnostico>> DiagnosticosAsync(CancellationToken ct) =>
        await db.Diagnosticos.OrderBy(d => d.Codigo).ToListAsync(ct);

    public Task<Encuentro?> EncuentroPorTurnoAsync(Guid turnoId, CancellationToken ct) =>
        db.Encuentros.FirstOrDefaultAsync(e => e.TurnoId == turnoId, ct);

    public Task<Encuentro?> ObtenerEncuentroAsync(Guid id, CancellationToken ct) =>
        db.Encuentros.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Encuentro> AgregarEncuentroAsync(Encuentro encuentro, CancellationToken ct)
    {
        db.Encuentros.Add(encuentro);
        await db.SaveChangesAsync(ct);
        return encuentro;
    }

    public async Task ReemplazarDiagnosticosAsync(Guid encuentroId, IReadOnlyList<Guid> diagnosticoIds, CancellationToken ct)
    {
        var actuales = await db.EncuentroDiagnosticos.Where(x => x.EncuentroId == encuentroId).ToListAsync(ct);
        db.EncuentroDiagnosticos.RemoveRange(actuales);
        foreach (var diagnosticoId in diagnosticoIds.Distinct())
            db.EncuentroDiagnosticos.Add(new EncuentroDiagnostico { EncuentroId = encuentroId, DiagnosticoId = diagnosticoId });
    }

    public async Task<IReadOnlyList<Encuentro>> EncuentrosDePacienteAsync(Guid pacienteId, CancellationToken ct) =>
        await db.Encuentros.Where(e => e.PacienteId == pacienteId).ToListAsync(ct);

    public async Task<IReadOnlyList<Diagnostico>> DiagnosticosDeEncuentroAsync(Guid encuentroId, CancellationToken ct) =>
        await db.EncuentroDiagnosticos.Where(x => x.EncuentroId == encuentroId).Select(x => x.Diagnostico!).ToListAsync(ct);

    public async Task<Receta> AgregarRecetaAsync(Receta receta, CancellationToken ct)
    {
        foreach (var item in receta.Items)
            item.RecetaId = receta.Id;
        db.Recetas.Add(receta);
        await db.SaveChangesAsync(ct);
        return receta;
    }

    public async Task<Receta?> ObtenerRecetaAsync(Guid id, CancellationToken ct) =>
        await db.Recetas.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Receta>> RecetasDePacienteAsync(Guid pacienteId, CancellationToken ct) =>
        await db.Recetas.Include(r => r.Items).Where(r => r.PacienteId == pacienteId).ToListAsync(ct);

    public async Task<IReadOnlyList<RecetaItem>> ItemsRecetaAsync(Guid recetaId, CancellationToken ct) =>
        await db.RecetaItems.Where(i => i.RecetaId == recetaId).ToListAsync(ct);

    async Task IClinicaStore.GuardarAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Arancel>> ArancelesAsync(CancellationToken ct) =>
        await db.Aranceles.OrderByDescending(a => a.VigenteDesde).ToListAsync(ct);

    public async Task<Arancel?> ArancelVigenteAsync(Guid tipoTurnoId, DateOnly fecha, CancellationToken ct) =>
        await db.Aranceles.Where(a => a.TipoTurnoId == tipoTurnoId && a.VigenteDesde <= fecha)
            .OrderByDescending(a => a.VigenteDesde).FirstOrDefaultAsync(ct);

    public async Task<Arancel> AgregarArancelAsync(Arancel arancel, CancellationToken ct)
    {
        db.Aranceles.Add(arancel);
        await db.SaveChangesAsync(ct);
        return arancel;
    }

    public Task<Comprobante?> ComprobantePorTurnoAsync(Guid turnoId, CancellationToken ct) =>
        db.Comprobantes.Include(c => c.Items).FirstOrDefaultAsync(c => c.TurnoId == turnoId, ct);

    public async Task<Comprobante> AgregarComprobanteAsync(Comprobante comprobante, CancellationToken ct)
    {
        foreach (var item in comprobante.Items)
            item.ComprobanteId = comprobante.Id;
        db.Comprobantes.Add(comprobante);
        await db.SaveChangesAsync(ct);
        return comprobante;
    }

    public Task<Comprobante?> ObtenerComprobanteAsync(Guid id, CancellationToken ct) =>
        db.Comprobantes.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Comprobante>> ComprobantesAsync(CancellationToken ct) =>
        await db.Comprobantes.Include(c => c.Items).OrderByDescending(c => c.CreadoEn).ToListAsync(ct);

    public async Task<IReadOnlyList<ComprobanteItem>> ItemsAsync(Guid comprobanteId, CancellationToken ct) =>
        await db.ComprobanteItems.Where(i => i.ComprobanteId == comprobanteId).ToListAsync(ct);

    async Task IFacturacionStore.GuardarAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);

    public async Task RegistrarAsync(Guid? usuarioId, string accion, string entidad, string? entidadId, string? detalle, CancellationToken ct)
    {
        db.Auditoria.Add(new AuditoriaEntrada
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle,
            Fecha = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditoriaEntrada>> ListarAsync(CancellationToken ct) =>
        await db.Auditoria.OrderByDescending(a => a.Fecha).Take(200).ToListAsync(ct);

    private async Task<IReadOnlyList<Turno>> DelDiaInternoAsync(DateOnly fecha, Guid? profesionalId, TimeZoneInfo zona, CancellationToken ct)
    {
        var inicio = AgendaRules.Combinar(fecha, TimeOnly.MinValue, zona);
        var fin = inicio.AddDays(1);
        var query = db.Turnos.Where(t => t.Inicio >= inicio && t.Inicio < fin);
        if (profesionalId is Guid id)
            query = query.Where(t => t.ProfesionalId == id);
        return await query.ToListAsync(ct);
    }
}
