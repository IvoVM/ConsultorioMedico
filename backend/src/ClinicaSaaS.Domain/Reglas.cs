namespace ClinicaSaaS.Domain;

public readonly record struct Intervalo(DateTimeOffset Inicio, DateTimeOffset Fin);

public static class SlugRules
{
    public static bool EsValido(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.Length <= 40
        && System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$");

    public static string NombreBase(string slug) => "tenant_" + slug.Replace('-', '_');
}

public static class AgendaRules
{
    public static readonly TimeSpan AnticipacionCancelacionPaciente = TimeSpan.FromHours(24);

    public static bool CuentaComoOcupado(EstadoTurno estado) =>
        estado is EstadoTurno.Reservado or EstadoTurno.Admitido or EstadoTurno.EnCurso or EstadoTurno.Completado;

    public static bool SeSolapan(Intervalo a, Intervalo b) => a.Inicio < b.Fin && b.Inicio < a.Fin;

    public static bool HaySolape(IEnumerable<(Intervalo Intervalo, EstadoTurno Estado)> existentes, Intervalo nuevo) =>
        existentes.Any(e => CuentaComoOcupado(e.Estado) && SeSolapan(e.Intervalo, nuevo));

    public static bool PuedeCancelarPaciente(EstadoTurno estado, DateTimeOffset inicio, DateTimeOffset ahora) =>
        estado == EstadoTurno.Reservado && inicio - ahora >= AnticipacionCancelacionPaciente;

    public static bool PuedeReprogramar(EstadoTurno estado) =>
        estado is EstadoTurno.Reservado or EstadoTurno.Admitido;

    public static IReadOnlyList<Intervalo> GenerarHuecos(
        DateOnly fecha,
        TimeOnly desde,
        TimeOnly hasta,
        int duracionMinutos,
        TimeZoneInfo zona,
        IEnumerable<Intervalo> ocupados)
    {
        if (duracionMinutos <= 0 || hasta <= desde)
            return [];

        var ocupadosLista = ocupados.ToList();
        var resultado = new List<Intervalo>();
        var cursor = desde;
        while (cursor.AddMinutes(duracionMinutos) <= hasta)
        {
            var inicio = Combinar(fecha, cursor, zona);
            var hueco = new Intervalo(inicio, inicio.AddMinutes(duracionMinutos));
            if (!ocupadosLista.Any(o => SeSolapan(o, hueco)))
                resultado.Add(hueco);
            cursor = cursor.AddMinutes(duracionMinutos);
        }

        return resultado;
    }

    public static DateTimeOffset Combinar(DateOnly fecha, TimeOnly hora, TimeZoneInfo zona)
    {
        var local = fecha.ToDateTime(hora);
        return new DateTimeOffset(local, zona.GetUtcOffset(local));
    }
}

public static class FacturacionRules
{
    public static decimal Total(IEnumerable<decimal> importes)
    {
        decimal total = 0;
        foreach (var importe in importes)
            total += importe;
        return total;
    }

    public static bool PuedePagar(EstadoComprobante estado) => estado == EstadoComprobante.Pendiente;
}

public static class EncuentroRules
{
    public static bool PuedeCerrar(string? nota) => !string.IsNullOrWhiteSpace(nota);
}
