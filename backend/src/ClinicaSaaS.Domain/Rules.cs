namespace ClinicaSaaS.Domain;

public readonly record struct TimeRange(DateTimeOffset Start, DateTimeOffset End);

public static class SlugRules
{
    public static bool IsValid(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.Length <= 40
        && System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$");
}

public static class SchedulingRules
{
    public static readonly TimeSpan PatientCancellationNotice = TimeSpan.FromHours(24);

    public static bool CountsAsBusy(AppointmentStatus status) =>
        status is AppointmentStatus.Booked or AppointmentStatus.Rescheduled or AppointmentStatus.CheckedIn or AppointmentStatus.InProgress or AppointmentStatus.Completed;

    public static bool Overlap(TimeRange a, TimeRange b) => a.Start < b.End && b.Start < a.End;

    public static bool HasOverlap(IEnumerable<(TimeRange Range, AppointmentStatus Status)> existing, TimeRange candidate) =>
        existing.Any(e => CountsAsBusy(e.Status) && Overlap(e.Range, candidate));

    public static bool PatientCanCancel(AppointmentStatus status, DateTimeOffset start, DateTimeOffset now) =>
        status is AppointmentStatus.Booked or AppointmentStatus.Rescheduled && start - now >= PatientCancellationNotice;

    public const int ExpiredRequestDays = 30;

    public static bool RequestExpired(DateTimeOffset start, DateTimeOffset now, TimeZoneInfo zone) =>
        LocalDay(start, zone) < LocalDay(now, zone);

    public static DateTimeOffset ActiveRequestsFrom(DateTimeOffset now, TimeZoneInfo zone) =>
        Combine(LocalDay(now, zone).AddDays(1 - ExpiredRequestDays), TimeOnly.MinValue, zone);

    public static bool RequestDeactivated(DateTimeOffset start, DateTimeOffset now, TimeZoneInfo zone) =>
        start < ActiveRequestsFrom(now, zone);

    private static DateOnly LocalDay(DateTimeOffset moment, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(moment, zone).DateTime);

    public static IReadOnlyList<TimeRange> MergeRanges(IEnumerable<TimeRange> ranges)
    {
        var merged = new List<TimeRange>();
        foreach (var range in ranges.Where(r => r.End > r.Start).OrderBy(r => r.Start))
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End)
            {
                var last = merged[^1];
                merged[^1] = new TimeRange(last.Start, range.End > last.End ? range.End : last.End);
            }
            else
            {
                merged.Add(range);
            }
        }
        return merged;
    }

    public static bool IsOverbook(IEnumerable<TimeRange> agenda, TimeRange candidate) =>
        !MergeRanges(agenda).Any(range => range.Start <= candidate.Start && candidate.End <= range.End);

    public static bool CanMoveOnAgenda(AppointmentStatus status) =>
        status is AppointmentStatus.Booked or AppointmentStatus.Rescheduled or AppointmentStatus.CheckedIn;

    public static bool CanReschedule(AppointmentStatus status) =>
        status is AppointmentStatus.Booked or AppointmentStatus.Rescheduled or AppointmentStatus.CheckedIn;

    public static IReadOnlyList<TimeRange> GenerateSlots(
        DateOnly date,
        TimeOnly from,
        TimeOnly to,
        int durationMinutes,
        TimeZoneInfo zone,
        IEnumerable<TimeRange> busy)
    {
        if (durationMinutes <= 0 || to <= from)
            return [];

        var busyList = busy.ToList();
        var result = new List<TimeRange>();
        var cursor = from;
        while (cursor.AddMinutes(durationMinutes) <= to)
        {
            var start = Combine(date, cursor, zone);
            var slot = new TimeRange(start, start.AddMinutes(durationMinutes));
            if (!busyList.Any(b => Overlap(b, slot)))
                result.Add(slot);
            cursor = cursor.AddMinutes(durationMinutes);
        }

        return result;
    }

    public static DateTimeOffset Combine(DateOnly date, TimeOnly time, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(time);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}

public static class BillingRules
{
    public static decimal Total(IEnumerable<decimal> amounts)
    {
        decimal total = 0;
        foreach (var amount in amounts)
            total += amount;
        return total;
    }

    public static bool CanPay(InvoiceStatus status) => status == InvoiceStatus.Pending;

    public static decimal NormalizePrice(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static string? CanonicalPayer(string? payer)
    {
        if (string.IsNullOrWhiteSpace(payer))
            return null;
        var text = payer.Trim();
        if (text.Equals("particular", StringComparison.OrdinalIgnoreCase))
            return null;
        return text.Length > 120 ? text[..120] : text;
    }

    public static string? PriceChangeError(decimal amount, decimal? current)
    {
        var rounded = NormalizePrice(amount);
        if (rounded < 0)
            return "El valor no puede ser negativo.";
        if (rounded == 0)
            return "El valor no puede ser 0.";
        if (current is decimal existing && NormalizePrice(existing) == rounded)
            return "El valor tiene que ser distinto al actual.";
        return null;
    }
}

public static class EncounterRules
{
    public static bool CanClose(string? note) => !string.IsNullOrWhiteSpace(note);
}
