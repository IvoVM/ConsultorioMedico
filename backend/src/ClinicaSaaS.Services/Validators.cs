using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.Utilities;
using FluentValidation;

namespace ClinicaSaaS.Services;

public class RegisterPatientValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientValidator()
    {
        RuleFor(x => x.Password)
            .Must(password => PasswordRules.Issue(password) is null)
            .WithMessage(command => PasswordRules.Issue(command.Password)!);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Phone)
            .Must(phone => PhoneRules.TryFormat(phone, out _))
            .WithMessage("El teléfono no es válido.");
    }
}

public class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.ProfessionalId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.AppointmentTypeId).NotEmpty();
        RuleFor(x => x.Start).NotEmpty();
    }
}

public class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator(TimeProvider clock)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Phone)
            .Must(phone => PhoneRules.TryFormat(phone, out _))
            .WithMessage("El teléfono no es válido.");
        RuleFor(x => x.BirthDate)
            .Must(d => d <= DateOnly.FromDateTime(clock.GetUtcNow().DateTime))
            .WithMessage("La fecha de nacimiento no puede ser futura.");
        RuleFor(x => x.HealthInsurance).MaximumLength(120);
        RuleFor(x => x.MemberNumber).MaximumLength(40);
        RuleFor(x => x.EmergencyContact).MaximumLength(120);
        RuleFor(x => x.EmergencyPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRules.TryFormat(phone, out _))
            .WithMessage("El teléfono no es válido.");
    }
}

public class SaveMedicalRecordValidator : AbstractValidator<SaveMedicalRecordCommand>
{
    public SaveMedicalRecordValidator(TimeProvider clock)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Phone)
            .Must(phone => PhoneRules.TryFormat(phone, out _))
            .WithMessage("El teléfono no es válido.");
        RuleFor(x => x.BirthDate)
            .Must(d => d <= DateOnly.FromDateTime(clock.GetUtcNow().DateTime))
            .WithMessage("La fecha de nacimiento no puede ser futura.");
        RuleFor(x => x.HealthInsurance).MaximumLength(120);
        RuleFor(x => x.MemberNumber).MaximumLength(40);
        RuleFor(x => x.EmergencyContact).MaximumLength(120);
        RuleFor(x => x.EmergencyPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRules.TryFormat(phone, out _))
            .WithMessage("El teléfono no es válido.");
    }
}

public class SaveEncounterValidator : AbstractValidator<SaveEncounterCommand>
{
    public SaveEncounterValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(4000);
        RuleFor(x => x.Plan).MaximumLength(4000);
        RuleFor(x => x.BloodPressure).MaximumLength(20);
        RuleFor(x => x.HeartRate).InclusiveBetween(20, 300).When(x => x.HeartRate is not null);
        RuleFor(x => x.Temperature).InclusiveBetween(30m, 45m).When(x => x.Temperature is not null);
        RuleFor(x => x.WeightKg).InclusiveBetween(0.3m, 400m).When(x => x.WeightKg is not null);
    }
}

public class AmendEntryValidator : AbstractValidator<AmendEntryCommand>
{
    public AmendEntryValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Escribí la corrección.").MaximumLength(4000);
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingresá tu contraseña para firmar.");
    }
}

public class AddBackgroundValidator : AbstractValidator<AddBackgroundCommand>
{
    public static readonly string[] BloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "0+", "0-"];

    public AddBackgroundValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Text).NotEmpty().WithMessage("Escribí el antecedente.").MaximumLength(2000);
        RuleFor(x => x.Text)
            .Must(text => BloodTypes.Contains(text.Trim().ToUpperInvariant()))
            .When(x => x.Kind == BackgroundKind.BloodType && !string.IsNullOrWhiteSpace(x.Text))
            .WithMessage("Grupo sanguíneo inválido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingresá tu contraseña para firmar.");
    }
}

public class AddConsentValidator : AbstractValidator<AddConsentCommand>
{
    public AddConsentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Procedure).NotEmpty().WithMessage("Indicá el procedimiento.").MaximumLength(2000);
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.FileName).NotEmpty().WithMessage("Adjuntá el consentimiento firmado por el paciente.").MaximumLength(200);
        RuleFor(x => x.ContentType)
            .Must(type => ClinicalLedger.AttachmentTypes.Contains(type))
            .WithMessage("El consentimiento tiene que ser PDF, JPG o PNG.");
        RuleFor(x => x.ContentBase64).NotEmpty().WithMessage("Adjuntá el consentimiento firmado por el paciente.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingresá tu contraseña para firmar.");
    }
}

public class SaveClinicSettingsValidator : AbstractValidator<SaveClinicSettingsCommand>
{
    public SaveClinicSettingsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        When(x => x.BillingEnabled && x.FiscalEnabled, () =>
        {
            RuleFor(x => x.TaxId).Must(BillingRules.IsValidTaxId).WithMessage("El CUIT no es válido.");
            RuleFor(x => x.TaxCondition).NotNull().WithMessage("Elegí la condición fiscal.");
            RuleFor(x => x.PointOfSale).NotNull().InclusiveBetween(1, 99999).WithMessage("El punto de venta va de 1 a 99999.");
        });
        RuleFor(x => x.MaxOverbooksPerDay).InclusiveBetween(0, 20).When(x => x.MaxOverbooksPerDay is not null)
            .WithMessage("Los sobreturnos por día van de 0 a 20, o dejalo vacío para no poner tope.");
    }
}
