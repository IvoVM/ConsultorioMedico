using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class FeeService(
    IBillingStore store,
    IOrganizationStore organization,
    ICurrentUser currentUser,
    IAuditStore audit,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<FeeDto>> FeesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var fees = await store.Fees
            .Where(f => f.EffectiveFrom <= today)
            .OrderByDescending(f => f.EffectiveFrom)
            .ThenByDescending(f => f.Id)
            .Select(FeeMappingExtensions.MapForFeeDto())
            .ToListAsync(ct);
        return fees
            .Select(fee => fee.MapToFeeDto())
            .GroupBy(f => (f.AppointmentTypeId, BillingRules.CanonicalPayer(f.Payer)?.ToLowerInvariant() ?? ""))
            .Select(g => g.First())
            .OrderBy(f => f.AppointmentType)
            .ThenBy(f => f.Payer)
            .ToList();
    }

    public async Task<object> FeesResultAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await FeesAsync(ct);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var source = store.FeeRows(today);
        if (query.Term is string term)
            source = source.Where(row => row.Service.ToLower().Contains(term));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("amount", false) => source.OrderBy(row => row.Amount ?? -1).ThenBy(row => row.Service),
            ("amount", true) => source.OrderByDescending(row => row.Amount ?? -1).ThenBy(row => row.Service),
            ("from", false) => source.OrderBy(row => row.EffectiveFrom ?? DateOnly.MinValue).ThenBy(row => row.Service),
            ("from", true) => source.OrderByDescending(row => row.EffectiveFrom ?? DateOnly.MinValue).ThenBy(row => row.Service),
            ("name", true) => source.OrderByDescending(row => row.Service),
            _ => source.OrderBy(row => row.Service)
        };
        var page = await Pages.Materialize(ordered, query, ct);
        return new PageDto<FeeRowDto>(page.Items.Select(row => row.MapToFeeRowDto()).ToList(), page.Total, page.Page, page.PageSize);
    }

    public async Task<FeeDto> CreateFeeAsync(CreateFeeCommand command, CancellationToken ct)
    {
        _ = await organization.GetAppointmentTypeAsync(command.AppointmentTypeId, ct)
            ?? throw new NotFoundException("Prestación no encontrada.");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var payer = BillingRules.CanonicalPayer(command.Payer);
        var current = await store.GetCurrentFeeAsync(command.AppointmentTypeId, today, payer, ct);
        var error = BillingRules.PriceChangeError(command.Amount, current?.Amount);
        if (error is not null)
            throw new BusinessRuleException(error);

        var amount = BillingRules.NormalizePrice(command.Amount);
        Fee fee;
        if (current is not null && current.EffectiveFrom == today)
        {
            current.Amount = amount;
            current.Payer = payer;
            await store.RemoveFeesEffectiveFromAsync(command.AppointmentTypeId, today, payer, current.Id, ct);
            await store.SaveAsync(ct);
            fee = current;
        }
        else
        {
            await store.RemoveFeesEffectiveFromAsync(command.AppointmentTypeId, today, payer, null, ct);
            fee = await store.AddFeeAsync(new Fee
            {
                Id = Guid.NewGuid(),
                AppointmentTypeId = command.AppointmentTypeId,
                Payer = payer,
                Amount = amount,
                EffectiveFrom = today
            }, ct);
        }

        await audit.RecordAsync(currentUser.Id, "arancel", "Arancel", fee.Id.ToString(), $"{fee.Payer ?? "Particular"} {fee.Amount:0.00}", ct);
        var saved = await store.Fees.Where(f => f.Id == fee.Id).Select(FeeMappingExtensions.MapForFeeDto()).FirstAsync(ct);
        return saved.MapToFeeDto();
    }
}
