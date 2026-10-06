using System.Text;
using System.Text.Json.Serialization;
using ClinicaSaaS.Api;
using Microsoft.AspNetCore.DataProtection;
using ClinicaSaaS.Application;
using ClinicaSaaS.Infrastructure;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["ZonaHoraria"] ?? "America/Argentina/Buenos_Aires");
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new FechaJsonConverter(zonaHoraria));
    options.JsonSerializerOptions.Converters.Add(new FechaNullableJsonConverter(zonaHoraria));
});
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta Jwt:Key.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name
    };
});
builder.Services.AddAuthorization();
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
    app.MapOpenApi();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
