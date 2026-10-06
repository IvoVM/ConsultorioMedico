using System.Text.Json;
using ClinicaSaaS.Application;
using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Api;

public class ApiExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (MapaErrores.Traducir(ex) is { } error)
        {
            await Write(context, error.Status, error.Mensaje);
        }
    }

    private static Task Write(HttpContext context, int status, string error)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error }));
    }
}

public class TenantMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, CatalogDbContext catalog, CurrentTenant current, ISecretProtector protector)
    {
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/platform", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/salud", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var slug = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(slug))
        {
            var host = context.Request.Host.Host;
            var dot = host.IndexOf('.');
            if (dot > 0 && host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
                slug = host[..dot];
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Falta el consultorio (encabezado X-Tenant-Slug)." });
            return;
        }

        var tenant = await catalog.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug.Trim().ToLowerInvariant());
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Consultorio no encontrado." });
            return;
        }

        if (tenant.Estado != EstadoTenant.Activo)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "El consultorio no está activo." });
            return;
        }

        var tokenSlug = context.User.FindFirst("tenant_slug")?.Value;
        if (tokenSlug is not null && !string.Equals(tokenSlug, tenant.Slug, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "El token no corresponde a este consultorio." });
            return;
        }

        current.Resolve(tenant.Slug, protector.Unprotect(tenant.ConnectionStringProtegida), tenant.Estado);
        await next(context);
    }
}
