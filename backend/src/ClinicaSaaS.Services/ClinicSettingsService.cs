using FluentValidation;
using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services;

public class ClinicSettingsService(
    IClinicSettingsStore store,
    ICurrentTenant clinic,
    ICurrentUser currentUser,
    IAuditStore audit,
    IValidator<SaveClinicSettingsCommand> validator)
{
    public async Task<ClinicSettingsDto> GetAsync(CancellationToken ct) => ToDto(await store.GetAsync(ct));

    public async Task<ClinicDto> ProfileAsync(CancellationToken ct)
    {
        var settings = await store.GetAsync(ct);
        return new ClinicDto(clinic.Slug, settings.Name, settings.BillingEnabled, settings.BillingEnabled && settings.FiscalEnabled);
    }

    public async Task<ClinicSettingsDto> SaveAsync(SaveClinicSettingsCommand command, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var settings = await store.GetAsync(ct);
        settings.Name = command.Name.Trim();
        settings.BillingEnabled = command.BillingEnabled;
        // Automatic and electronic invoices only make sense while the clinic issues invoices at all.
        settings.AutoInvoice = command.BillingEnabled && command.AutoInvoice;
        settings.FiscalEnabled = command.BillingEnabled && command.FiscalEnabled;
        // The tax data is kept even with electronic invoicing off, so turning it back on needs no retyping.
        settings.TaxId = string.IsNullOrWhiteSpace(command.TaxId) ? null : new string(command.TaxId.Where(char.IsDigit).ToArray());
        settings.TaxCondition = command.TaxCondition;
        settings.PointOfSale = command.PointOfSale;
        await store.SaveAsync(ct);
        var detail = $"{settings.Name} · facturación {(settings.BillingEnabled ? "sí" : "no")} · automática {(settings.AutoInvoice ? "sí" : "no")} · electrónica {(settings.FiscalEnabled ? "sí" : "no")}";
        await audit.RecordAsync(currentUser.Id, "edicion", "Configuracion", settings.Id.ToString(), detail, ct);
        return ToDto(settings);
    }

    private static ClinicSettingsDto ToDto(ClinicSettings settings) =>
        new(settings.Name, settings.BillingEnabled, settings.AutoInvoice, settings.FiscalEnabled, settings.TaxId, settings.TaxCondition, settings.PointOfSale);
}
