using ClinicaSaaS.Domain;
using ClinicaSaaS.Services;

namespace ClinicaSaaS.Infrastructure.Fiscal;

// A real clinic until its ARCA certificate is wired in (WSAA and FECAESolicitar): it cannot authorize anything,
// because a made-up CAE handed to a real patient is a false fiscal document. Its invoices stay internal and print
// "Documento no válido como factura"; the fiscal ones are issued from ARCA's own site meanwhile.
public class UnconnectedFiscalAuthority : IFiscalAuthority
{
    public bool Available => false;

    public Task<FiscalAuthorization> AuthorizeAsync(FiscalRequest request, CancellationToken ct) =>
        throw new BusinessRuleException("La factura electrónica todavía no está conectada con ARCA en este consultorio. Emití la factura desde ARCA.");
}

// A real clinic until it has an account with a card processor: nothing to reconcile against, so nothing is made up.
public class UnconnectedCardProcessor : ICardProcessor
{
    public bool Simulated => false;
    public bool Available => false;

    public Task<IReadOnlyList<SettlementLineData>> SettlementAsync(CardBrand brand, DateOnly from, DateOnly to, CancellationToken ct) =>
        throw new BusinessRuleException("La liquidación de tarjetas todavía no está conectada con el procesador en este consultorio.");
}
