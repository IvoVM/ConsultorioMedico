using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateTenantCommand>, CreateTenantValidator>();
        services.AddScoped<IValidator<RegisterPatientCommand>, RegisterPatientValidator>();
        services.AddScoped<IValidator<BookAppointmentCommand>, BookAppointmentValidator>();
        services.AddScoped<IValidator<SaveEncounterCommand>, SaveEncounterValidator>();
        services.AddScoped<IValidator<CreatePrescriptionCommand>, CreatePrescriptionValidator>();
        services.AddScoped<IValidator<SaveMedicalRecordCommand>, SaveMedicalRecordValidator>();
        services.AddScoped<TenantsService>();
        services.AddScoped<PlatformAuthService>();
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
