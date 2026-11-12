using FluentValidation;

namespace ClinicaSaaS.Services;

public class ClinicSettingsService(
    IClinicSettingsStore store,
    ICurrentTenant clinic,
    ICurrentUser currentUser,
    IAuditStore audit,
    IValidator<SaveClinicSettingsCommand> validator)
{
    public async Task<ClinicSettingsDto> GetAsync(CancellationToken ct)
    {
        var settings = await store.GetAsync(ct);
        return new ClinicSettingsDto(settings.Name, settings.BillingEnabled, settings.AutoInvoice);
    }

    public async Task<ClinicDto> ProfileAsync(CancellationToken ct)
    {
        var settings = await store.GetAsync(ct);
        return new ClinicDto(clinic.Slug, settings.Name, settings.BillingEnabled);
    }

    public async Task<ClinicSettingsDto> SaveAsync(SaveClinicSettingsCommand command, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var settings = await store.GetAsync(ct);
        settings.Name = command.Name.Trim();
        settings.BillingEnabled = command.BillingEnabled;
        // Automatic invoices only make sense while the clinic issues invoices at all.
        settings.AutoInvoice = command.BillingEnabled && command.AutoInvoice;
        await store.SaveAsync(ct);
        var detail = $"{settings.Name} · facturación {(settings.BillingEnabled ? "sí" : "no")} · automática {(settings.AutoInvoice ? "sí" : "no")}";
        await audit.RecordAsync(currentUser.Id, "edicion", "Configuracion", settings.Id.ToString(), detail, ct);
        return new ClinicSettingsDto(settings.Name, settings.BillingEnabled, settings.AutoInvoice);
    }
}
