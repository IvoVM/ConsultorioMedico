using System.Linq.Expressions;
using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class FeeMappingExtensions
{
    public static FeeDto MapToFeeDto(this Fee fee) =>
        new(fee.Id, fee.AppointmentTypeId, fee.AppointmentType.Name, fee.Amount, fee.EffectiveFrom, fee.Payer);

    public static Expression<Func<Fee, Fee>> MapForFeeDto() =>
        fee => new Fee
        {
            Id = fee.Id,
            AppointmentTypeId = fee.AppointmentTypeId,
            Amount = fee.Amount,
            EffectiveFrom = fee.EffectiveFrom,
            Payer = fee.Payer,
            AppointmentType = new AppointmentType
            {
                Name = fee.AppointmentType.Name
            }
        };
}
