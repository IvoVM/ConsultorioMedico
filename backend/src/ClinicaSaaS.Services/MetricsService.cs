using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The admin dashboard: how the clinic did over the last 7, 30 or 90 days, against the period right before.
// It answers three questions only: are we seeing patients, are we losing appointments, is the money coming in.
// Clinical content (notes, diagnoses) stays out, since the admin does not read clinical history.
public class MetricsService(
    IAppointmentStore appointments,
    IScheduleStore schedules,
    IEmployeeStore employees,
    IBillingStore billing,
    IClinicSettingsStore settings,
    ITimeZoneProvider timeZone,
    TimeProvider clock)
{
    private const int TopRows = 8;

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
        var bucketDays = range == MetricsRange.Quarter ? 7 : 1;
        var to = SchedulingRules.LocalDate(clock.GetUtcNow(), zone);
        var from = to.AddDays(1 - days);
        var previousFrom = from.AddDays(-days);
        var start = SchedulingRules.Combine(from, TimeOnly.MinValue, zone);
        var end = SchedulingRules.Combine(to.AddDays(1), TimeOnly.MinValue, zone);
        var previousStart = SchedulingRules.Combine(previousFrom, TimeOnly.MinValue, zone);

        var visits = await appointments.Appointments
            .Where(a => a.Start >= previousStart && a.Start < end)
            .Select(a => new Visit(
                a.Start,
                a.End,
                a.Status,
                a.ProfessionalId,
                (a.Professional.FirstName + " " + a.Professional.LastName).Trim(),
                a.AppointmentType.Specialty != null ? a.AppointmentType.Specialty.Name : null))
            .ToListAsync(ct);
        var current = visits.Where(v => v.Start >= start).ToList();
        var previous = visits.Where(v => v.Start < start).ToList();

        var firstVisits = await appointments.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .GroupBy(a => a.ClientId)
            .Select(g => g.Min(a => a.Start))
            .Where(first => first >= previousStart && first < end)
            .ToListAsync(ct);

        var occupancy = await OccupancyAsync(previousFrom, from, to, visits, start, ct);

        var kpis = new MetricsKpisDto(
            new(Count(current, AppointmentStatus.Completed), Count(previous, AppointmentStatus.Completed)),
            new(NoShowRate(current), NoShowRate(previous)),
            new(occupancy.Current, occupancy.Previous),
            new(firstVisits.Count(f => f >= start), firstVisits.Count(f => f < start)));

        var series = Buckets(from, days, bucketDays)
            .Select(date =>
            {
                var inBucket = current.Where(v => BucketOf(v.Start, from, bucketDays, zone) == date).ToList();
                return new AppointmentBucketDto(
                    date,
                    Count(inBucket, AppointmentStatus.Completed),
                    Count(inBucket, AppointmentStatus.NoShow),
                    Count(inBucket, AppointmentStatus.Cancelled),
                    inBucket.Count(v => IsPending(v.Status)));
            })
            .ToList();

        var heatmap = current
            .Where(v => v.Status != AppointmentStatus.Cancelled)
            .Select(v => TimeZoneInfo.ConvertTime(v.Start, zone))
            .GroupBy(local => (local.DayOfWeek, local.Hour))
            .Select(g => new HeatCellDto(g.Key.DayOfWeek, g.Key.Hour, g.Count()))
            .OrderBy(c => c.Day)
            .ThenBy(c => c.Hour)
            .ToList();

        var clinic = await settings.GetAsync(ct);
        var billingMetrics = clinic.BillingEnabled
            ? await BillingAsync(from, days, bucketDays, start, end, previousStart, ct)
            : null;

        return new MetricsDto(
            range,
            from,
            to,
            bucketDays,
            kpis,
            series,
            Rank(current, v => v.Professional),
            Rank(current, v => v.Specialty ?? "Sin especialidad"),
            heatmap,
            billingMetrics);
    }

    private async Task<BillingMetricsDto> BillingAsync(
        DateOnly from, int days, int bucketDays, DateTimeOffset start, DateTimeOffset end, DateTimeOffset previousStart, CancellationToken ct)
    {
        var zone = timeZone.Zone;
        var issued = await billing.Invoices
            .Where(i => i.Status != InvoiceStatus.Voided && i.CreatedAt >= start && i.CreatedAt < end)
            .Select(i => new { i.CreatedAt, i.Total })
            .ToListAsync(ct);
        var paid = await billing.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= previousStart && i.PaidAt < end)
            .Select(i => new { PaidAt = i.PaidAt!.Value, i.Total })
            .ToListAsync(ct);
        var outstanding = await billing.Invoices
            .Where(i => i.Status == InvoiceStatus.Pending)
            .SumAsync(i => i.Total, ct);

        var currentPaid = paid.Where(i => i.PaidAt >= start).ToList();
        var series = Buckets(from, days, bucketDays)
            .Select(date => new BillingBucketDto(
                date,
                issued.Where(i => BucketOf(i.CreatedAt, from, bucketDays, zone) == date).Sum(i => i.Total),
                currentPaid.Where(i => BucketOf(i.PaidAt, from, bucketDays, zone) == date).Sum(i => i.Total)))
            .ToList();

        return new BillingMetricsDto(
            new(currentPaid.Sum(i => i.Total), paid.Where(i => i.PaidAt < start).Sum(i => i.Total)),
            outstanding,
            series);
    }

    // Share of the active professionals' weekly hours, minus their blockouts, that ended up booked.
    private async Task<(decimal? Current, decimal? Previous)> OccupancyAsync(
        DateOnly previousFrom, DateOnly from, DateOnly to, IReadOnlyList<Visit> visits, DateTimeOffset start, CancellationToken ct)
    {
        var zone = timeZone.Zone;
        var blocks = await schedules.Blocks
            .Where(b => employees.Employees.Any(e => e.UserId == b.ProfessionalId && e.IsActive))
            .Select(b => new { b.ProfessionalId, b.Day, b.StartTime, b.EndTime })
            .ToListAsync(ct);
        if (blocks.Count == 0) return (null, null);
        var professionals = blocks.Select(b => b.ProfessionalId).ToHashSet();
        var windowStart = SchedulingRules.Combine(previousFrom, TimeOnly.MinValue, zone);
        var windowEnd = SchedulingRules.Combine(to.AddDays(1), TimeOnly.MinValue, zone);
        var blockouts = await schedules.Blockouts
            .Where(b => professionals.Contains(b.ProfessionalId) && b.Start < windowEnd && b.End > windowStart)
            .Select(b => new { b.ProfessionalId, b.Start, b.End })
            .ToListAsync(ct);

        double Available(DateOnly first, DateOnly last)
        {
            double minutes = 0;
            for (var date = first; date <= last; date = date.AddDays(1))
                foreach (var block in blocks.Where(b => b.Day == date.DayOfWeek))
                {
                    var open = new TimeRange(SchedulingRules.Combine(date, block.StartTime, zone), SchedulingRules.Combine(date, block.EndTime, zone));
                    var closed = blockouts
                        .Where(o => o.ProfessionalId == block.ProfessionalId)
                        .Sum(o => Math.Max(0, (Min(open.End, o.End) - Max(open.Start, o.Start)).TotalMinutes));
                    minutes += Math.Max(0, (open.End - open.Start).TotalMinutes - closed);
                }
            return minutes;
        }

        double Booked(IEnumerable<Visit> list) => list
            .Where(v => professionals.Contains(v.ProfessionalId) && v.Status != AppointmentStatus.Cancelled)
            .Sum(v => (v.End - v.Start).TotalMinutes);

        decimal? Share(double booked, double available) =>
            available <= 0 ? null : Math.Round((decimal)Math.Min(100, booked / available * 100), 1);

        return (
            Share(Booked(visits.Where(v => v.Start >= start)), Available(from, to)),
            Share(Booked(visits.Where(v => v.Start < start)), Available(previousFrom, from.AddDays(-1))));
    }

    private static IEnumerable<DateOnly> Buckets(DateOnly from, int days, int bucketDays)
    {
        for (var offset = 0; offset < days; offset += bucketDays)
            yield return from.AddDays(offset);
    }

    private static DateOnly BucketOf(DateTimeOffset moment, DateOnly from, int bucketDays, TimeZoneInfo zone)
    {
        var offset = SchedulingRules.LocalDate(moment, zone).DayNumber - from.DayNumber;
        return from.AddDays(offset - offset % bucketDays);
    }

    private static List<RankRowDto> Rank(IEnumerable<Visit> visits, Func<Visit, string> label) => visits
        .GroupBy(label)
        .Select(g => new RankRowDto(g.Key, Count(g, AppointmentStatus.Completed), Count(g, AppointmentStatus.NoShow)))
        .Where(r => r.Completed + r.NoShow > 0)
        .OrderByDescending(r => r.Completed)
        .ThenBy(r => r.Label)
        .Take(TopRows)
        .ToList();

    private static bool IsPending(AppointmentStatus status) =>
        status is AppointmentStatus.Booked or AppointmentStatus.Rescheduled or AppointmentStatus.CheckedIn or AppointmentStatus.InProgress;

    private static int Count(IEnumerable<Visit> visits, AppointmentStatus status) => visits.Count(v => v.Status == status);

    // A rate with nothing to divide by has no value; the dashboard shows a dash instead of a misleading 0 %.
    private static decimal? NoShowRate(IReadOnlyList<Visit> visits)
    {
        var noShow = Count(visits, AppointmentStatus.NoShow);
        var whole = Count(visits, AppointmentStatus.Completed) + noShow;
        return whole == 0 ? null : Math.Round(noShow * 100m / whole, 1);
    }

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    private sealed record Visit(
        DateTimeOffset Start,
        DateTimeOffset End,
        AppointmentStatus Status,
        Guid ProfessionalId,
        string Professional,
        string? Specialty);
}
