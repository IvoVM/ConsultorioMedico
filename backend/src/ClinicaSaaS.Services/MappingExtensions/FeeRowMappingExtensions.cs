using ClinicaSaaS.Domain.QueryViews;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class FeeRowMappingExtensions
{
    public static FeeRowDto MapToFeeRowDto(this FeeRow row) =>
        new(row.AppointmentTypeId, row.Service, row.Amount, row.EffectiveFrom);
}
