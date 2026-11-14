using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Persistence;
using ClinicaSaaS.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure.Fiscal;

// Stands in for the card processor's settlement files until the clinic has an account with one: it settles
// the card payments taken at the desk, keeps a made-up fee (1.8 % in one payment, 3.5 % in installments)
// and pays them two days later in one payment or eighteen days later in installments. Payments not paid out
// yet are left out, as the real file would. A real client implements ICardProcessor and nothing else changes.
public class SimulatedCardProcessor(TenantDbContext db, ITimeZoneProvider timeZone, TimeProvider clock) : ICardProcessor
{
    public bool Simulated => true;

    public async Task<IReadOnlyList<SettlementLineData>> SettlementAsync(CardBrand brand, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var zone = timeZone.Zone;
        var start = SchedulingRules.Combine(from, TimeOnly.MinValue, zone);
        var end = SchedulingRules.Combine(to.AddDays(1), TimeOnly.MinValue, zone);
        var today = SchedulingRules.LocalDate(clock.GetUtcNow(), zone);
        var charges = await db.Receipts.AsNoTracking()
            .Where(r => r.CardBrand == brand && r.Amount > 0 && r.PaidAt >= start && r.PaidAt < end && r.CardBatch != null && r.CardCoupon != null)
            .Select(r => new { r.PaidAt, r.Amount, r.CardBatch, r.CardCoupon, Installments = r.CardInstallments ?? 1 })
            .ToListAsync(ct);
        return charges
            .Select(c =>
            {
                var charged = SchedulingRules.LocalDate(c.PaidAt, zone);
                var paid = charged.AddDays(c.Installments == 1 ? 2 : 18);
                var fee = decimal.Round(c.Amount * (c.Installments == 1 ? 0.018m : 0.035m), 2, MidpointRounding.AwayFromZero);
                return new SettlementLineData(c.CardBatch!, c.CardCoupon!, c.Installments, charged, paid, c.Amount, fee);
            })
            .Where(l => l.PaidOn <= today)
            .ToList();
    }
}
