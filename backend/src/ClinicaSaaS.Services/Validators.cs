using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Services;

public class RegisterPatientValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
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

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.EncounterId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Medication).NotEmpty().MaximumLength(120);
            item.RuleFor(i => i.Dose).NotEmpty().MaximumLength(60);
            item.RuleFor(i => i.Frequency).NotEmpty().MaximumLength(60);
            item.RuleFor(i => i.Duration).NotEmpty().MaximumLength(60);
        });
    }
}

public class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator(TimeProvider clock)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BirthDate)
            .Must(d => d <= DateOnly.FromDateTime(clock.GetUtcNow().DateTime))
            .WithMessage("La fecha de nacimiento no puede ser futura.");
        RuleFor(x => x.HealthInsurance).MaximumLength(120);
        RuleFor(x => x.MemberNumber).MaximumLength(40);
        RuleFor(x => x.EmergencyContact).MaximumLength(120);
        RuleFor(x => x.EmergencyPhone).MaximumLength(30);
    }
}

public class SaveMedicalRecordValidator : AbstractValidator<SaveMedicalRecordCommand>
{
    public static readonly string[] BloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "0+", "0-"];

    public SaveMedicalRecordValidator(TimeProvider clock)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BirthDate)
            .Must(d => d <= DateOnly.FromDateTime(clock.GetUtcNow().DateTime))
            .WithMessage("La fecha de nacimiento no puede ser futura.");
        RuleFor(x => x.BloodType)
            .Must(b => string.IsNullOrWhiteSpace(b) || BloodTypes.Contains(b.Trim()))
            .WithMessage("Grupo sanguíneo inválido.");
        RuleFor(x => x.Allergies).MaximumLength(1000);
        RuleFor(x => x.PersonalHistory).MaximumLength(2000);
        RuleFor(x => x.FamilyHistory).MaximumLength(2000);
        RuleFor(x => x.CurrentMedication).MaximumLength(1000);
        RuleFor(x => x.Habits).MaximumLength(1000);
        RuleFor(x => x.HealthInsurance).MaximumLength(120);
        RuleFor(x => x.MemberNumber).MaximumLength(40);
        RuleFor(x => x.EmergencyContact).MaximumLength(120);
        RuleFor(x => x.EmergencyPhone).MaximumLength(30);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class SaveEncounterValidator : AbstractValidator<SaveEncounterCommand>
{
    public SaveEncounterValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(4000);
    }
}
