using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The admin dashboard for the last 7, 30 or 90 days: what share of appointments were seen, missed, cancelled or moved,
// who saw them, and how much was invoiced and collected. Clinical content stays out, since the admin does not read it.
public class MetricsService(
    IAppointmentStore appointments,
    IBillingStore billing,
    IClinicSettingsStore settings,
    ITimeZoneProvider timeZone,
    TimeProvider clock)
{
    public async Task<MetricsDto> MetricsAsync(MetricsRange range, CancellationToken ct)
    {
        var zone = timeZone.Zone;
        var days = range switch
        {
            MetricsRange.Week => 7,
            MetricsRange.Month => 30,
            MetricsRange.Quarter => 90,
            _ => throw new BusinessRuleException("Elegí 7, 30 o 90 días.")
        };
        var to = SchedulingRules.LocalDate(clock.GetUtcNow(), zone);
        var from = to.AddDays(1 - days);
        var start = SchedulingRules.Combine(from, TimeOnly.MinValue, zone);
        var end = SchedulingRules.Combine(to.AddDays(1), TimeOnly.MinValue, zone);

        var visits = await appointments.Appointments
            .Where(a => a.Start >= start && a.Start < end)
            .Select(a => new
            {
                a.Status,
                Professional = (a.Professional.FirstName + " " + a.Professional.LastName).Trim(),
                // Every move leaves a Rescheduled row in the trail, so a moved appointment still counts once it is seen.
                Moved = a.Status == AppointmentStatus.Rescheduled
                    || appointments.Changes.Any(c => c.AppointmentId == a.Id && c.Status == AppointmentStatus.Rescheduled)
            })
            .ToListAsync(ct);

        var total = visits.Count;
        StatusShareDto Share(int count) => new(count, total == 0 ? null : Math.Round(count * 100m / total, 1));
        var summary = new AppointmentMetricsDto(
            total,
            Share(visits.Count(v => v.Status == AppointmentStatus.Completed)),
            Share(visits.Count(v => v.Status == AppointmentStatus.NoShow)),
            Share(visits.Count(v => v.Status == AppointmentStatus.Cancelled)),
            Share(visits.Count(v => v.Moved)));

        var professionals = visits
            .Where(v => v.Status == AppointmentStatus.Completed)
            .GroupBy(v => v.Professional)
            .Select(g => new RankRowDto(g.Key, g.Count()))
            .OrderByDescending(r => r.Completed)
            .ThenBy(r => r.Label)
            .ToList();

        var clinic = await settings.GetAsync(ct);
        var billingMetrics = clinic.BillingEnabled ? await BillingAsync(from, days, range, start, end, ct) : null;

        return new MetricsDto(range, from, to, summary, professionals, billingMetrics);
    }

    private async Task<BillingMetricsDto> BillingAsync(
        DateOnly from, int days, MetricsRange range, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var zone = timeZone.Zone;
        // Ninety daily points are too many to read, so the long range is drawn by week.
        var bucketDays = range == MetricsRange.Quarter ? 7 : 1;
        var issued = await billing.Invoices
            .Where(i => i.Status != InvoiceStatus.Voided && i.CreatedAt >= start && i.CreatedAt < end)
            .Select(i => new { i.CreatedAt, i.Total })
            .ToListAsync(ct);
        var paid = await billing.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= start && i.PaidAt < end)
            .Select(i => new { PaidAt = i.PaidAt!.Value, i.Total })
            .ToListAsync(ct);
        var outstanding = await billing.Invoices
            .Where(i => i.Status == InvoiceStatus.Pending)
            .SumAsync(i => i.Total, ct);

        DateOnly BucketOf(DateTimeOffset moment)
        {
            var offset = SchedulingRules.LocalDate(moment, zone).DayNumber - from.DayNumber;
            return from.AddDays(offset - offset % bucketDays);
        }

        var series = new List<BillingBucketDto>();
        for (var offset = 0; offset < days; offset += bucketDays)
        {
            var date = from.AddDays(offset);
            series.Add(new BillingBucketDto(
                date,
                issued.Where(i => BucketOf(i.CreatedAt) == date).Sum(i => i.Total),
                paid.Where(i => BucketOf(i.PaidAt) == date).Sum(i => i.Total)));
        }

        return new BillingMetricsDto(issued.Sum(i => i.Total), paid.Sum(i => i.Total), outstanding, bucketDays, series);
    }
}
