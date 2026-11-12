using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Route("api")]
public class ClinicController(ClinicSettingsService settings) : ApiController
{
    [AllowAnonymous]
    [HttpGet("clinica")]
    public Task<IActionResult> Get(CancellationToken ct) => Run(() => settings.ProfileAsync(ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpGet("configuracion")]
    public Task<IActionResult> Settings(CancellationToken ct) => Run(() => settings.GetAsync(ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPut("configuracion")]
    public Task<IActionResult> SaveSettings(SaveClinicSettingsCommand command, CancellationToken ct) =>
        Run(() => settings.SaveAsync(command, ct));
}
