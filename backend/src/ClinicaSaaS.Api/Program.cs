using System.Text.Json.Serialization;
using ClinicaSaaS.Api;
using Microsoft.AspNetCore.DataProtection;
using ClinicaSaaS.Application;
using ClinicaSaaS.Infrastructure;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var timeZone = TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["TimeZone"] ?? "America/Argentina/Buenos_Aires");
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new LocalDateTimeOffsetJsonConverter(timeZone));
    options.JsonSerializerOptions.Converters.Add(new NullableLocalDateTimeOffsetJsonConverter(timeZone));
});
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddClinicaJwt(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.SetIsOriginAllowed(origin =>
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;
        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("ClinicaSaaS");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await catalog.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<SuperAdminSeeder>().SeedAsync(CancellationToken.None);
    if (args.Contains("migrate-tenants"))
    {
        await scope.ServiceProvider.GetRequiredService<ITenantMigrator>().MigrateAllAsync(CancellationToken.None);
        return;
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
