using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.Tests;

public class SchedulingRulesTests
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    [Fact]
    public void Detects_overlap_with_active_appointments()
    {
        var existing = new[]
        {
            (new TimeRange(At(9, 0), At(9, 30)), AppointmentStatus.Booked),
            (new TimeRange(At(10, 0), At(10, 30)), AppointmentStatus.Cancelled)
        };

        Assert.True(SchedulingRules.HasOverlap(existing, new TimeRange(At(9, 15), At(9, 45))));
        Assert.False(SchedulingRules.HasOverlap(existing, new TimeRange(At(10, 0), At(10, 30))));
        Assert.False(SchedulingRules.HasOverlap(existing, new TimeRange(At(9, 30), At(10, 0))));
    }

    [Fact]
    public void Generates_slots_and_skips_busy_ranges()
    {
        var busy = new TimeRange(At(9, 30), At(10, 0));
        var slots = SchedulingRules.GenerateSlots(
            new DateOnly(2026, 10, 5),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            30,
            Zone,
            [busy]);

        Assert.Equal(3, slots.Count);
        Assert.DoesNotContain(slots, s => s.Start == busy.Start);
    }

    [Fact]
    public void Patient_cancels_only_booked_appointments_with_24_hours_notice()
    {
        var start = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        Assert.True(SchedulingRules.PatientCanCancel(AppointmentStatus.Booked, start, start.AddHours(-24)));
        Assert.False(SchedulingRules.PatientCanCancel(AppointmentStatus.Booked, start, start.AddHours(-23)));
        Assert.False(SchedulingRules.PatientCanCancel(AppointmentStatus.CheckedIn, start, start.AddHours(-48)));
    }

    private static DateTimeOffset At(int hour, int minute) =>
        SchedulingRules.Combine(new DateOnly(2026, 10, 5), new TimeOnly(hour, minute), Zone);
}

public class BillingAndEncounterRulesTests
{
    [Fact]
    public void Total_adds_up_amounts()
    {
        Assert.Equal(3500m, BillingRules.Total([1500m, 2000m]));
        Assert.True(BillingRules.CanPay(InvoiceStatus.Pending));
        Assert.False(BillingRules.CanPay(InvoiceStatus.Paid));
    }

    [Fact]
    public void Closing_an_encounter_requires_a_note()
    {
        Assert.False(EncounterRules.CanClose("  "));
        Assert.True(EncounterRules.CanClose("Control de presión."));
    }

    [Fact]
    public void Slug_accepts_lowercase_segments()
    {
        Assert.True(SlugRules.IsValid("san-martin"));
        Assert.False(SlugRules.IsValid("San Martin"));
    }
}
