using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application;

public class BillingService(
    IBillingStore store,
    IOrganizationStore organization,
    IClientStore clients,
    ITenantUserStore users,
    ICurrentUser currentUser,
    IAuditStore audit,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<FeeDto>> FeesAsync(CancellationToken ct)
    {
        var types = await organization.AppointmentTypesAsync(ct);
        return (await store.FeesAsync(ct))
            .OrderByDescending(f => f.EffectiveFrom)
            .Select(f => new FeeDto(f.Id, f.AppointmentTypeId, types.FirstOrDefault(t => t.Id == f.AppointmentTypeId)?.Name ?? "", f.Amount, f.EffectiveFrom))
            .ToList();
    }

    public async Task<FeeDto> CreateFeeAsync(CreateFeeCommand command, CancellationToken ct)
    {
        if (command.Amount < 0)
            throw new BusinessRuleException("El monto no puede ser negativo.");
        var type = await organization.GetAppointmentTypeAsync(command.AppointmentTypeId, ct)
            ?? throw new NotFoundException("Tipo de turno no encontrado.");
        var fee = await store.AddFeeAsync(new Fee
        {
            Id = Guid.NewGuid(),
            AppointmentTypeId = command.AppointmentTypeId,
            Amount = command.Amount,
            EffectiveFrom = command.EffectiveFrom
        }, ct);
        await audit.RecordAsync(currentUser.Id, "arancel", "Arancel", fee.Id.ToString(), fee.Amount.ToString("0.00"), ct);
        return new FeeDto(fee.Id, fee.AppointmentTypeId, type.Name, fee.Amount, fee.EffectiveFrom);
    }

    public async Task<IReadOnlyList<InvoiceDto>> InvoicesAsync(CancellationToken ct)
    {
        var list = await store.InvoicesAsync(ct);
        var dtos = new List<InvoiceDto>();
        foreach (var invoice in list.OrderByDescending(i => i.CreatedAt))
            dtos.Add(await MapAsync(invoice, ct));
        return dtos;
    }

    public async Task<InvoiceDto> PayAsync(Guid id, PayInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await store.GetInvoiceAsync(id, ct) ?? throw new NotFoundException("Comprobante no encontrado.");
        if (!BillingRules.CanPay(invoice.Status))
            throw new BusinessRuleException("Ese comprobante no admite un pago.");
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaymentMethod = command.Method;
        invoice.PaidAt = clock.GetUtcNow();
        invoice.Total = BillingRules.Total((await store.InvoiceItemsAsync(invoice.Id, ct)).Select(i => i.Amount));
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "pago", "Comprobante", invoice.Id.ToString(), command.Method.ToString(), ct);
        return await MapAsync(invoice, ct);
    }

    private async Task<InvoiceDto> MapAsync(Invoice invoice, CancellationToken ct)
    {
        var client = await clients.GetAsync(invoice.ClientId, ct);
        var account = client is null ? null : await users.FindByIdAsync(client.UserId, ct);
        var items = invoice.Items.Count > 0
            ? invoice.Items
            : (await store.InvoiceItemsAsync(invoice.Id, ct)).ToList();
        return new InvoiceDto(
            invoice.Id,
            invoice.AppointmentId,
            invoice.ClientId,
            account?.Name ?? "Paciente",
            invoice.Total,
            invoice.Status,
            invoice.PaymentMethod,
            invoice.CreatedAt,
            invoice.PaidAt,
            items.Select(i => new InvoiceItemDto(i.Description, i.Amount)).ToList());
    }
}
