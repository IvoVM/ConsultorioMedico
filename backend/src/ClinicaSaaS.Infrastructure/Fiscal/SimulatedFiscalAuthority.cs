using System.Security.Cryptography;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Persistence;
using ClinicaSaaS.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure.Fiscal;

// Stands in for ARCA's WSFE service until the clinic has a certificate: it numbers each voucher type per point of sale
// after the last one this clinic issued, as FECompUltimoAutorizado would, and returns a 14-digit CAE valid for ten days.
// A real client implements IFiscalAuthority with WSAA login and FECAESolicitar, and nothing else changes.
public class SimulatedFiscalAuthority(TenantDbContext db) : IFiscalAuthority
{
    public async Task<FiscalAuthorization> AuthorizeAsync(FiscalRequest request, CancellationToken ct)
    {
        var last = request.Type is FiscalVoucherType.NotaCreditoB or FiscalVoucherType.NotaCreditoC
            ? await db.Invoices
                .Where(i => i.PointOfSale == request.PointOfSale && i.CreditNoteType == request.Type)
                .MaxAsync(i => i.CreditNoteNumber, ct)
            : await db.Invoices
                .Where(i => i.PointOfSale == request.PointOfSale && i.VoucherType == request.Type)
                .MaxAsync(i => i.Number, ct);
        var cae = string.Concat(Enumerable.Range(0, 14).Select(_ => RandomNumberGenerator.GetInt32(10)));
        return new FiscalAuthorization((last ?? 0) + 1, cae, request.Date.AddDays(10), Simulated: true);
    }
}
