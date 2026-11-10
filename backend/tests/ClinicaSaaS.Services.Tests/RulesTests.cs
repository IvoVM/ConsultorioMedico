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
    public void Local_date_uses_the_clinic_zone_whatever_offset_arrives()
    {
        // 22:00 in Buenos Aires is 01:00 UTC of the next day.
        var utc = new DateTimeOffset(2026, 11, 10, 1, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 11, 9), SchedulingRules.LocalDate(utc, Zone));
        Assert.Equal(new DateOnly(2026, 11, 9), SchedulingRules.LocalDate(utc.ToOffset(TimeSpan.FromHours(-3)), Zone));
    }

    [Fact]
    public void Moving_keeps_a_patient_in_the_waiting_room()
    {
        Assert.Equal(AppointmentStatus.CheckedIn, SchedulingRules.StatusAfterMove(AppointmentStatus.CheckedIn));
        Assert.Equal(AppointmentStatus.Rescheduled, SchedulingRules.StatusAfterMove(AppointmentStatus.Booked));
        Assert.Equal(AppointmentStatus.Rescheduled, SchedulingRules.StatusAfterMove(AppointmentStatus.Rescheduled));
    }

    [Fact]
    public void Unattended_booking_becomes_absent_after_the_grace()
    {
        var end = At(10, 0);

        Assert.False(SchedulingRules.BecomesNoShow(AppointmentStatus.Booked, end, end + SchedulingRules.NoShowGrace - TimeSpan.FromMinutes(1)));
        Assert.True(SchedulingRules.BecomesNoShow(AppointmentStatus.Booked, end, end + SchedulingRules.NoShowGrace));
        Assert.True(SchedulingRules.BecomesNoShow(AppointmentStatus.Rescheduled, end, end.AddHours(5)));
        Assert.False(SchedulingRules.BecomesNoShow(AppointmentStatus.CheckedIn, end, end.AddHours(5)));
        Assert.False(SchedulingRules.BecomesNoShow(AppointmentStatus.Completed, end, end.AddHours(5)));
    }

    [Fact]
    public void Absent_patient_arriving_the_same_day_can_still_check_in()
    {
        var start = SchedulingRules.Combine(new DateOnly(2026, 11, 9), new TimeOnly(9, 0), Zone);
        var sameDay = SchedulingRules.Combine(new DateOnly(2026, 11, 9), new TimeOnly(18, 0), Zone);
        var nextDay = SchedulingRules.Combine(new DateOnly(2026, 11, 10), new TimeOnly(8, 0), Zone);

        Assert.True(SchedulingRules.CanCheckIn(AppointmentStatus.NoShow, start, sameDay, Zone));
        Assert.False(SchedulingRules.CanCheckIn(AppointmentStatus.NoShow, start, nextDay, Zone));
        Assert.True(SchedulingRules.CanCheckIn(AppointmentStatus.Booked, start, nextDay, Zone));
        Assert.False(SchedulingRules.CanCheckIn(AppointmentStatus.Cancelled, start, sameDay, Zone));
    }

    [Fact]
    public void Request_expires_once_its_day_has_passed_in_the_clinic_zone()
    {
        var now = SchedulingRules.Combine(new DateOnly(2026, 10, 5), new TimeOnly(15, 0), Zone);

        Assert.True(SchedulingRules.RequestExpired(SchedulingRules.Combine(new DateOnly(2026, 10, 4), new TimeOnly(18, 0), Zone), now, Zone));
        Assert.False(SchedulingRules.RequestExpired(SchedulingRules.Combine(new DateOnly(2026, 10, 5), new TimeOnly(9, 0), Zone), now, Zone));
        Assert.False(SchedulingRules.RequestExpired(SchedulingRules.Combine(new DateOnly(2026, 10, 6), new TimeOnly(9, 0), Zone), now, Zone));
    }

    [Fact]
    public void Expired_request_is_deactivated_after_thirty_days()
    {
        var now = SchedulingRules.Combine(new DateOnly(2026, 10, 31), new TimeOnly(15, 0), Zone);

        Assert.False(SchedulingRules.RequestDeactivated(SchedulingRules.Combine(new DateOnly(2026, 10, 2), new TimeOnly(0, 0), Zone), now, Zone));
        Assert.True(SchedulingRules.RequestDeactivated(SchedulingRules.Combine(new DateOnly(2026, 10, 1), new TimeOnly(23, 30), Zone), now, Zone));
        Assert.False(SchedulingRules.RequestDeactivated(SchedulingRules.Combine(new DateOnly(2026, 10, 30), new TimeOnly(9, 0), Zone), now, Zone));
    }

    [Fact]
    public void Turn_outside_the_agenda_is_an_overbook()
    {
        var agenda = new[]
        {
            new TimeRange(At(9, 0), At(10, 0)),
            new TimeRange(At(10, 0), At(12, 0)),
            new TimeRange(At(15, 0), At(17, 0)),
        };

        Assert.False(SchedulingRules.IsOverbook(agenda, new TimeRange(At(9, 45), At(10, 15))));
        Assert.False(SchedulingRules.IsOverbook(agenda, new TimeRange(At(15, 0), At(17, 0))));
        Assert.True(SchedulingRules.IsOverbook(agenda, new TimeRange(At(11, 45), At(12, 15))));
        Assert.True(SchedulingRules.IsOverbook(agenda, new TimeRange(At(13, 0), At(13, 30))));
        Assert.True(SchedulingRules.IsOverbook([], new TimeRange(At(9, 0), At(9, 30))));
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
    public void Price_change_rejects_zero_negative_and_the_same_amount()
    {
        Assert.Equal("El valor no puede ser negativo.", BillingRules.PriceChangeError(-1, 100));
        Assert.Equal("El valor no puede ser 0.", BillingRules.PriceChangeError(0, 100));
        Assert.Equal("El valor tiene que ser distinto al actual.", BillingRules.PriceChangeError(100, 100));
        Assert.Null(BillingRules.PriceChangeError(120, 100));
        Assert.Null(BillingRules.PriceChangeError(80, null));
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
