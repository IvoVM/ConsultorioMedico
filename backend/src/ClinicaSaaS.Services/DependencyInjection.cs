using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name switch
        {
            "Email" => "email",
            "Password" => "contraseña",
            "FirstName" => "nombre",
            "LastName" => "apellido",
            "DocumentNumber" => "documento",
            "Phone" => "teléfono",
            "BirthDate" => "fecha de nacimiento",
            "HealthInsurance" => "obra social",
            "MemberNumber" => "número de afiliado",
            "EmergencyContact" => "contacto de emergencia",
            "EmergencyPhone" => "teléfono de emergencia",
            "BloodType" => "grupo sanguíneo",
            "Allergies" => "alergias",
            "PersonalHistory" => "antecedentes personales",
            "FamilyHistory" => "antecedentes familiares",
            "CurrentMedication" => "medicación actual",
            "Habits" => "hábitos",
            "Notes" => "notas",
            "ProfessionalId" => "profesional",
            "LocationId" => "sede",
            "AppointmentTypeId" => "prestación",
            "Start" => "horario",
            "AppointmentId" => "turno",
            "Note" => "nota",
            "EncounterId" => "consulta",
            "Items" => "receta",
            "Medication" => "medicamento",
            "Dose" => "dosis",
            "Frequency" => "frecuencia",
            "Duration" => "duración",
            "Instructions" => "indicaciones",
            _ => null
        };
        services.AddScoped<IValidator<RegisterPatientCommand>, RegisterPatientValidator>();
        services.AddScoped<IValidator<BookAppointmentCommand>, BookAppointmentValidator>();
        services.AddScoped<IValidator<SaveEncounterCommand>, SaveEncounterValidator>();
        services.AddScoped<IValidator<CreatePrescriptionCommand>, CreatePrescriptionValidator>();
        services.AddScoped<IValidator<SaveMedicalRecordCommand>, SaveMedicalRecordValidator>();
        services.AddScoped<IValidator<CreatePatientCommand>, CreatePatientValidator>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<EmployeesService>();
        services.AddScoped<UsersService>();
        services.AddScoped<ScheduleService>();
        services.AddScoped<ClinicalService>();
        services.AddScoped<BillingService>();
        services.AddScoped<MedicalRecordsService>();
        return services;
    }
}
