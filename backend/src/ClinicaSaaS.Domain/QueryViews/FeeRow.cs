namespace ClinicaSaaS.Domain.QueryViews;

public sealed class FeeRow
{
    public Guid AppointmentTypeId { get; init; }
    public string Service { get; init; } = "";
    public decimal? Amount { get; init; }
    public DateOnly? EffectiveFrom { get; init; }
}
