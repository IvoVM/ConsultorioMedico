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
        status is AppointmentStatus.Booked or AppointmentStatus.CheckedIn or AppointmentStatus.InProgress or AppointmentStatus.Completed;

    public static bool Overlap(TimeRange a, TimeRange b) => a.Start < b.End && b.Start < a.End;

    public static bool HasOverlap(IEnumerable<(TimeRange Range, AppointmentStatus Status)> existing, TimeRange candidate) =>
        existing.Any(e => CountsAsBusy(e.Status) && Overlap(e.Range, candidate));

    public static bool PatientCanCancel(AppointmentStatus status, DateTimeOffset start, DateTimeOffset now) =>
        status == AppointmentStatus.Booked && start - now >= PatientCancellationNotice;

    public static bool CanReschedule(AppointmentStatus status) =>
        status is AppointmentStatus.Booked or AppointmentStatus.CheckedIn;

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
}

public static class EncounterRules
{
    public static bool CanClose(string? note) => !string.IsNullOrWhiteSpace(note);
}
