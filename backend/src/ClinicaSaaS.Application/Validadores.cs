using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class AltaTenantValidator : AbstractValidator<AltaTenantCommand>
{
    public AltaTenantValidator()
    {
        RuleFor(x => x.Slug).Must(SlugRules.EsValido).WithMessage("El slug solo admite minúsculas, números y guiones.");
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Tipo).IsInEnum();
    }
}

public class RegistroPacienteValidator : AbstractValidator<RegistroPacienteCommand>
{
    public RegistroPacienteValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Documento).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Telefono).NotEmpty().MaximumLength(30);
    }
}

public class ReservarTurnoValidator : AbstractValidator<ReservarTurnoCommand>
{
    public ReservarTurnoValidator()
    {
        RuleFor(x => x.ProfesionalId).NotEmpty();
        RuleFor(x => x.SedeId).NotEmpty();
        RuleFor(x => x.TipoTurnoId).NotEmpty();
        RuleFor(x => x.Inicio).NotEmpty();
    }
}

public class CrearRecetaValidator : AbstractValidator<CrearRecetaCommand>
{
    public CrearRecetaValidator()
    {
        RuleFor(x => x.EncuentroId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Medicamento).NotEmpty().MaximumLength(120);
            item.RuleFor(i => i.Dosis).NotEmpty().MaximumLength(60);
            item.RuleFor(i => i.Frecuencia).NotEmpty().MaximumLength(60);
            item.RuleFor(i => i.Duracion).NotEmpty().MaximumLength(60);
        });
    }
}

public class GuardarHistoriaMedicaValidator : AbstractValidator<GuardarHistoriaMedicaCommand>
{
    public static readonly string[] GruposSanguineos = ["A+", "A-", "B+", "B-", "AB+", "AB-", "0+", "0-"];

    public GuardarHistoriaMedicaValidator(TimeProvider clock)
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Documento).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Telefono).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FechaNacimiento)
            .Must(f => f <= DateOnly.FromDateTime(clock.GetUtcNow().DateTime))
            .WithMessage("La fecha de nacimiento no puede ser futura.");
        RuleFor(x => x.GrupoSanguineo)
            .Must(g => string.IsNullOrWhiteSpace(g) || GruposSanguineos.Contains(g.Trim()))
            .WithMessage("Grupo sanguíneo inválido.");
        RuleFor(x => x.Alergias).MaximumLength(1000);
        RuleFor(x => x.AntecedentesPersonales).MaximumLength(2000);
        RuleFor(x => x.AntecedentesFamiliares).MaximumLength(2000);
        RuleFor(x => x.MedicacionHabitual).MaximumLength(1000);
        RuleFor(x => x.Habitos).MaximumLength(1000);
        RuleFor(x => x.ObraSocial).MaximumLength(120);
        RuleFor(x => x.NumeroAfiliado).MaximumLength(40);
        RuleFor(x => x.ContactoEmergencia).MaximumLength(120);
        RuleFor(x => x.TelefonoEmergencia).MaximumLength(30);
        RuleFor(x => x.Observaciones).MaximumLength(2000);
    }
}

public class GuardarEncuentroValidator : AbstractValidator<GuardarEncuentroCommand>
{
    public GuardarEncuentroValidator()
    {
        RuleFor(x => x.TurnoId).NotEmpty();
        RuleFor(x => x.Nota).MaximumLength(4000);
    }
}
