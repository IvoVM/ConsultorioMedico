using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application.Tests;

public class AgendaRulesTests
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    [Fact]
    public void Detecta_solape_de_turnos_activos()
    {
        var existentes = new[]
        {
            (new Intervalo(Instante(9, 0), Instante(9, 30)), EstadoTurno.Reservado),
            (new Intervalo(Instante(10, 0), Instante(10, 30)), EstadoTurno.Cancelado)
        };

        Assert.True(AgendaRules.HaySolape(existentes, new Intervalo(Instante(9, 15), Instante(9, 45))));
        Assert.False(AgendaRules.HaySolape(existentes, new Intervalo(Instante(10, 0), Instante(10, 30))));
        Assert.False(AgendaRules.HaySolape(existentes, new Intervalo(Instante(9, 30), Instante(10, 0))));
    }

    [Fact]
    public void Genera_huecos_y_omite_ocupados()
    {
        var ocupado = new Intervalo(Instante(9, 30), Instante(10, 0));
        var huecos = AgendaRules.GenerarHuecos(
            new DateOnly(2026, 10, 5),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            30,
            Zona,
            [ocupado]);

        Assert.Equal(3, huecos.Count);
        Assert.DoesNotContain(huecos, h => h.Inicio == ocupado.Inicio);
    }

    [Fact]
    public void Paciente_cancela_solo_con_24_horas_y_turno_reservado()
    {
        var inicio = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        Assert.True(AgendaRules.PuedeCancelarPaciente(EstadoTurno.Reservado, inicio, inicio.AddHours(-24)));
        Assert.False(AgendaRules.PuedeCancelarPaciente(EstadoTurno.Reservado, inicio, inicio.AddHours(-23)));
        Assert.False(AgendaRules.PuedeCancelarPaciente(EstadoTurno.Admitido, inicio, inicio.AddHours(-48)));
    }

    private static DateTimeOffset Instante(int hora, int minuto) =>
        AgendaRules.Combinar(new DateOnly(2026, 10, 5), new TimeOnly(hora, minuto), Zona);
}

public class FacturacionYClinicaRulesTests
{
    [Fact]
    public void Total_suma_los_importes()
    {
        Assert.Equal(3500m, FacturacionRules.Total([1500m, 2000m]));
        Assert.True(FacturacionRules.PuedePagar(EstadoComprobante.Pendiente));
        Assert.False(FacturacionRules.PuedePagar(EstadoComprobante.Pagado));
    }

    [Fact]
    public void Cierre_de_encuentro_exige_nota()
    {
        Assert.False(EncuentroRules.PuedeCerrar("  "));
        Assert.True(EncuentroRules.PuedeCerrar("Control de presión."));
    }

    [Fact]
    public void Slug_valido_define_el_nombre_de_base()
    {
        Assert.True(SlugRules.EsValido("san-martin"));
        Assert.False(SlugRules.EsValido("San Martin"));
        Assert.Equal("tenant_san_martin", SlugRules.NombreBase("san-martin"));
    }
}

public class AltaTenantTests
{
    [Fact]
    public async Task Aprovisiona_la_base_y_guarda_la_connection_cifrada()
    {
        var catalogo = new CatalogoFalso();
        var provisioner = new ProvisionerFalso();
        var service = new TenantsService(
            new AltaTenantValidator(),
            catalogo,
            provisioner,
            new ProtectorFalso(),
            new FakeTime());

        var dto = await service.CrearAsync(new AltaTenantCommand("san-martin", "San Martín", TipoTenant.Consultorio), CancellationToken.None);

        Assert.Equal("san-martin", dto.Slug);
        Assert.Equal("san-martin", provisioner.Slug);
        var guardado = Assert.Single(catalogo.Tenants);
        Assert.Equal("enc:Host=tenant_san_martin", guardado.ConnectionStringProtegida);
        Assert.Equal(EstadoTenant.Activo, guardado.Estado);
    }

    [Fact]
    public async Task Rechaza_un_slug_invalido_sin_aprovisionar()
    {
        var provisioner = new ProvisionerFalso();
        var service = new TenantsService(new AltaTenantValidator(), new CatalogoFalso(), provisioner, new ProtectorFalso(), new FakeTime());

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.CrearAsync(new AltaTenantCommand("San Martin", "X", TipoTenant.Hospital), CancellationToken.None));
        Assert.Null(provisioner.Slug);
    }

    private sealed class FakeTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 4, 15, 0, 0, TimeSpan.Zero);
    }

    private sealed class ProtectorFalso : ISecretProtector
    {
        public string Protect(string value) => "enc:" + value;
        public string Unprotect(string value) => value["enc:".Length..];
    }

    private sealed class ProvisionerFalso : ITenantProvisioner
    {
        public string? Slug { get; private set; }
        public Task<string> ProvisionAsync(string slug, CancellationToken ct)
        {
            Slug = slug;
            return Task.FromResult("Host=" + SlugRules.NombreBase(slug));
        }
    }

    private sealed class CatalogoFalso : ICatalogStore
    {
        public List<Tenant> Tenants { get; } = [];
        public Task<bool> ExisteSlugAsync(string slug, CancellationToken ct) => Task.FromResult(Tenants.Any(t => t.Slug == slug));
        public Task AgregarAsync(Tenant tenant, CancellationToken ct)
        {
            Tenants.Add(tenant);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Tenant>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Tenant>>(Tenants);
        public Task<Tenant?> ObtenerPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Tenants.FirstOrDefault(t => t.Id == id));
        public Task<Tenant?> ObtenerPorSlugAsync(string slug, CancellationToken ct) => Task.FromResult(Tenants.FirstOrDefault(t => t.Slug == slug));
        public Task GuardarAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
