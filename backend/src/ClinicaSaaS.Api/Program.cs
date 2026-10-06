using System.Text.Json.Serialization;
using ClinicaSaaS.Api;
using ClinicaSaaS.Application;
using ClinicaSaaS.Infrastructure;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
if (origins.Length == 0)
    throw new InvalidOperationException("Falta Cors:Origins con las URL de este consultorio.");

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
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ClinicSeeder>().SeedAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<ClinicTokenMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
