using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class BillingService(
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
            .ToFeeDtos()
            .ToListAsync(ct);
        return fees
            .GroupBy(f => f.AppointmentTypeId)
            .Select(g => g.First())
            .OrderBy(f => f.AppointmentType)
            .ToList();
    }

    public async Task<FeeDto> CreateFeeAsync(CreateFeeCommand command, CancellationToken ct)
    {
        var type = await organization.GetAppointmentTypeAsync(command.AppointmentTypeId, ct)
            ?? throw new NotFoundException("Tipo de turno no encontrado.");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var current = await store.GetCurrentFeeAsync(command.AppointmentTypeId, today, ct);
        var error = BillingRules.PriceChangeError(command.Amount, current?.Amount);
        if (error is not null)
            throw new BusinessRuleException(error);

        var amount = BillingRules.NormalizePrice(command.Amount);
        Fee fee;
        if (current is not null && current.EffectiveFrom == today)
        {
            current.Amount = amount;
            await store.RemoveFeesEffectiveFromAsync(command.AppointmentTypeId, today, current.Id, ct);
            await store.SaveAsync(ct);
            fee = current;
        }
        else
        {
            await store.RemoveFeesEffectiveFromAsync(command.AppointmentTypeId, today, null, ct);
            fee = await store.AddFeeAsync(new Fee
            {
                Id = Guid.NewGuid(),
                AppointmentTypeId = command.AppointmentTypeId,
                Amount = amount,
                EffectiveFrom = today
            }, ct);
        }

        await audit.RecordAsync(currentUser.Id, "arancel", "Arancel", fee.Id.ToString(), fee.Amount.ToString("0.00"), ct);
        return new FeeDto(fee.Id, fee.AppointmentTypeId, type.Name, fee.Amount, fee.EffectiveFrom);
    }

    public async Task<IReadOnlyList<InvoiceDto>> InvoicesAsync(CancellationToken ct) =>
        await store.Invoices.OrderByDescending(i => i.CreatedAt).ToInvoiceDtos().ToListAsync(ct);

    public async Task<InvoiceDto> PayAsync(Guid id, PayInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (!BillingRules.CanPay(invoice.Status))
            throw new BusinessRuleException("Ese comprobante no admite un pago.");
        var amounts = await store.Invoices.Where(i => i.Id == invoice.Id).SelectMany(i => i.Items).Select(i => i.Amount).ToListAsync(ct);
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaymentMethod = command.Method;
        invoice.PaidAt = clock.GetUtcNow();
        invoice.Total = BillingRules.Total(amounts);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "pago", "Comprobante", invoice.Id.ToString(), command.Method.ToString(), ct);
        return await store.Invoices.Where(i => i.Id == invoice.Id).ToInvoiceDtos().FirstAsync(ct);
    }
}
