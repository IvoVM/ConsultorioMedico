using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application.Tests;

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
    public void Valid_slug_defines_the_database_name()
    {
        Assert.True(SlugRules.IsValid("san-martin"));
        Assert.False(SlugRules.IsValid("San Martin"));
        Assert.Equal("tenant_san_martin", SlugRules.DatabaseName("san-martin"));
    }
}

public class CreateTenantTests
{
    [Fact]
    public async Task Provisions_the_database_and_stores_the_encrypted_connection()
    {
        var catalog = new FakeCatalog();
        var provisioner = new FakeProvisioner();
        var service = new TenantsService(
            new CreateTenantValidator(),
            catalog,
            provisioner,
            new FakeProtector(),
            new FakeTime());

        var dto = await service.CreateAsync(new CreateTenantCommand("san-martin", "San Martín", TenantType.Practice), CancellationToken.None);

        Assert.Equal("san-martin", dto.Slug);
        Assert.Equal("san-martin", provisioner.Slug);
        var saved = Assert.Single(catalog.Tenants);
        Assert.Equal("enc:Host=tenant_san_martin", saved.ProtectedConnectionString);
        Assert.Equal(TenantStatus.Active, saved.Status);
    }

    [Fact]
    public async Task Rejects_an_invalid_slug_without_provisioning()
    {
        var provisioner = new FakeProvisioner();
        var service = new TenantsService(new CreateTenantValidator(), new FakeCatalog(), provisioner, new FakeProtector(), new FakeTime());

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.CreateAsync(new CreateTenantCommand("San Martin", "X", TenantType.Hospital), CancellationToken.None));
        Assert.Null(provisioner.Slug);
    }

    private sealed class FakeTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 4, 15, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeProtector : ISecretProtector
    {
        public string Protect(string value) => "enc:" + value;
        public string Unprotect(string value) => value["enc:".Length..];
    }

    private sealed class FakeProvisioner : ITenantProvisioner
    {
        public string? Slug { get; private set; }
        public Task<string> ProvisionAsync(string slug, CancellationToken ct)
        {
            Slug = slug;
            return Task.FromResult("Host=" + SlugRules.DatabaseName(slug));
        }
    }

    private sealed class FakeCatalog : ICatalogStore
    {
        public List<Tenant> Tenants { get; } = [];
        public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) => Task.FromResult(Tenants.Any(t => t.Slug == slug));
        public Task AddAsync(Tenant tenant, CancellationToken ct)
        {
            Tenants.Add(tenant);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Tenant>>(Tenants);
        public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Tenants.FirstOrDefault(t => t.Id == id));
        public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct) => Task.FromResult(Tenants.FirstOrDefault(t => t.Slug == slug));
        public Task SaveAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
