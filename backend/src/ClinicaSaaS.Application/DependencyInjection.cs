using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<AltaTenantCommand>, AltaTenantValidator>();
        services.AddScoped<IValidator<RegistroPacienteCommand>, RegistroPacienteValidator>();
        services.AddScoped<IValidator<ReservarTurnoCommand>, ReservarTurnoValidator>();
        services.AddScoped<IValidator<GuardarEncuentroCommand>, GuardarEncuentroValidator>();
        services.AddScoped<IValidator<CrearRecetaCommand>, CrearRecetaValidator>();
        services.AddScoped<TenantsService>();
        services.AddScoped<PlatformAuthService>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrganizacionService>();
        services.AddScoped<EmpleadosService>();
        services.AddScoped<AgendaService>();
        services.AddScoped<ClinicaService>();
        services.AddScoped<FacturacionService>();
        return services;
    }
}
