using System.Text.Json;
using ClinicaSaaS.Application;

namespace ClinicaSaaS.Api;

public class ApiExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ErrorMap.Translate(ex) is { } error)
        {
            await Write(context, error.Status, error.Message);
        }
    }

    private static Task Write(HttpContext context, int status, string error)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error }));
    }
}

public class ClinicTokenMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ICurrentTenant clinic)
    {
        var tokenSlug = context.User.FindFirst(AuthClaims.TenantSlug)?.Value;
        if (tokenSlug is not null && !string.Equals(tokenSlug, clinic.Slug, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "El token no corresponde a este consultorio." });
            return;
        }

        await next(context);
    }
}
