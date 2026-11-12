using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The admin dashboard: how the clinic did over the last 7, 30 or 90 days, against the period right before.
// Clinical content (notes, diagnoses) stays out, since the admin does not read clinical history.
public class MetricsService(
    IAppointmentStore appointments,
    IScheduleStore schedules,
    IEmployeeStore employees,
    IBookingRequestStore requests,
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
                a.ClientId,
                a.ProfessionalId,
                (a.Professional.FirstName + " " + a.Professional.LastName).Trim(),
                a.AppointmentType.Specialty != null ? a.AppointmentType.Specialty.Name : null,
                a.InsurerId != null && a.CoverageAccepted != false && a.Insurer != null ? a.Insurer.Name : null))
            .ToListAsync(ct);
        var current = visits.Where(v => v.Start >= start).ToList();
        var previous = visits.Where(v => v.Start < start).ToList();

        var firstVisits = await appointments.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .GroupBy(a => a.ClientId)
            .Select(g => g.Min(a => a.Start))
            .Where(first => first >= previousStart && first < end)
            .ToListAsync(ct);

        var requestRows = await requests.Entries
            .Where(r => r.CreatedAt >= previousStart && r.CreatedAt < end)
            .Select(r => new { r.CreatedAt, r.Status })
            .ToListAsync(ct);
        var currentRequests = requestRows.Where(r => r.CreatedAt >= start).Select(r => r.Status).ToList();
        var previousRequests = requestRows.Where(r => r.CreatedAt < start).Select(r => r.Status).ToList();

        var occupancy = await OccupancyAsync(previousFrom, from, to, visits, start, ct);

        var kpis = new MetricsKpisDto(
            Kpi(current.Count, previous.Count),
            Kpi(Count(current, AppointmentStatus.Completed), Count(previous, AppointmentStatus.Completed)),
            Rate(NoShowRate(current), NoShowRate(previous)),
            Rate(CancellationRate(current), CancellationRate(previous)),
            Kpi(Patients(current), Patients(previous)),
            Kpi(firstVisits.Count(f => f >= start), firstVisits.Count(f => f < start)),
            Rate(occupancy.Current, occupancy.Previous),
            Kpi(currentRequests.Count, previousRequests.Count),
            Rate(ApprovalRate(currentRequests), ApprovalRate(previousRequests)));

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

        var professionals = Rank(current, v => v.Professional);
        var specialties = Rank(current, v => v.Specialty ?? "Sin especialidad");

        var held = current.Where(v => v.Status != AppointmentStatus.Cancelled).ToList();
        var coverage = new CoverageMetricsDto(
            held.Count(v => v.Insurer != null),
            held.Count(v => v.Insurer == null),
            held.Where(v => v.Insurer != null)
                .GroupBy(v => v.Insurer!)
                .Select(g => new ShareDto(g.Key, g.Count()))
                .OrderByDescending(s => s.Count)
                .ThenBy(s => s.Label)
                .Take(TopRows)
                .ToList());

        var heatmap = held
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

        return new MetricsDto(range, from, to, bucketDays, kpis, series, professionals, specialties, coverage, heatmap, billingMetrics);
    }

    private async Task<BillingMetricsDto> BillingAsync(
        DateOnly from, int days, int bucketDays, DateTimeOffset start, DateTimeOffset end, DateTimeOffset previousStart, CancellationToken ct)
    {
        var zone = timeZone.Zone;
        var issued = await billing.Invoices
            .Where(i => i.Status != InvoiceStatus.Voided && i.CreatedAt >= previousStart && i.CreatedAt < end)
            .Select(i => new { i.CreatedAt, i.Total })
            .ToListAsync(ct);
        var paid = await billing.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= previousStart && i.PaidAt < end)
            .Select(i => new { PaidAt = i.PaidAt!.Value, i.Total, i.PaymentMethod })
            .ToListAsync(ct);
        var outstanding = await billing.Invoices
            .Where(i => i.Status == InvoiceStatus.Pending)
            .SumAsync(i => i.Total, ct);

        var currentIssued = issued.Where(i => i.CreatedAt >= start).ToList();
        var previousIssued = issued.Where(i => i.CreatedAt < start).ToList();
        var currentPaid = paid.Where(i => i.PaidAt >= start).ToList();
        var previousPaid = paid.Where(i => i.PaidAt < start).ToList();

        var series = Buckets(from, days, bucketDays)
            .Select(date => new BillingBucketDto(
                date,
                currentIssued.Where(i => BucketOf(i.CreatedAt, from, bucketDays, zone) == date).Sum(i => i.Total),
                currentPaid.Where(i => BucketOf(i.PaidAt, from, bucketDays, zone) == date).Sum(i => i.Total)))
            .ToList();
        var payments = currentPaid
            .Where(i => i.PaymentMethod != null)
            .GroupBy(i => i.PaymentMethod!.Value)
            .Select(g => new PaymentShareDto(g.Key, g.Sum(i => i.Total), g.Count()))
            .OrderByDescending(p => p.Amount)
            .ToList();

        return new BillingMetricsDto(
            Kpi(currentIssued.Sum(i => i.Total), previousIssued.Sum(i => i.Total)),
            Kpi(currentPaid.Sum(i => i.Total), previousPaid.Sum(i => i.Total)),
            Rate(Average(currentIssued.Select(i => i.Total)), Average(previousIssued.Select(i => i.Total))),
            outstanding,
            series,
            payments);
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

    private static int Patients(IEnumerable<Visit> visits) =>
        visits.Where(v => v.Status == AppointmentStatus.Completed).Select(v => v.ClientId).Distinct().Count();

    private static decimal? NoShowRate(IReadOnlyList<Visit> visits) =>
        Percent(Count(visits, AppointmentStatus.NoShow), Count(visits, AppointmentStatus.Completed) + Count(visits, AppointmentStatus.NoShow));

    private static decimal? CancellationRate(IReadOnlyList<Visit> visits) =>
        Percent(Count(visits, AppointmentStatus.Cancelled), visits.Count);

    private static decimal? ApprovalRate(IReadOnlyList<BookingRequestStatus> statuses) =>
        Percent(
            statuses.Count(s => s is BookingRequestStatus.Approved or BookingRequestStatus.Rescheduled),
            statuses.Count(s => s != BookingRequestStatus.Pending));

    private static decimal? Percent(int part, int whole) => whole == 0 ? null : Math.Round(part * 100m / whole, 1);

    private static decimal? Average(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 2);
    }

    private static MetricKpiDto Kpi(decimal value, decimal previous) => new(value, previous);

    // A rate with nothing to divide by has no value; the dashboard shows a dash instead of a misleading 0 %.
    private static MetricKpiDto Rate(decimal? value, decimal? previous) => new(value, previous);

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    private sealed record Visit(
        DateTimeOffset Start,
        DateTimeOffset End,
        AppointmentStatus Status,
        Guid ClientId,
        Guid ProfessionalId,
        string Professional,
        string? Specialty,
        string? Insurer);
}
