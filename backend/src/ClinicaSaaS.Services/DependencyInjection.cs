using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterPatientCommand>, RegisterPatientValidator>();
        services.AddScoped<IValidator<BookAppointmentCommand>, BookAppointmentValidator>();
        services.AddScoped<IValidator<SaveEncounterCommand>, SaveEncounterValidator>();
        services.AddScoped<IValidator<CreatePrescriptionCommand>, CreatePrescriptionValidator>();
        services.AddScoped<IValidator<SaveMedicalRecordCommand>, SaveMedicalRecordValidator>();
        services.AddScoped<IValidator<CreatePatientCommand>, CreatePatientValidator>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<EmployeesService>();
        services.AddScoped<ScheduleService>();
        services.AddScoped<ClinicalService>();
        services.AddScoped<BillingService>();
        services.AddScoped<MedicalRecordsService>();
        return services;
    }
}
