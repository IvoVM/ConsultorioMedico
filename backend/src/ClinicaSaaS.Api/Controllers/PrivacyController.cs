using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

// The patient's privacy center: what they agreed to, a copy of their data and closing the account.
[ApiController]
[Authorize(Roles = AppRoles.Patient)]
[Route("api/cuenta/privacidad")]
public class PrivacyController(PatientPrivacyService privacy) : ApiController
{
    [HttpGet]
    public Task<IActionResult> Center(CancellationToken ct) => Run(() => privacy.CenterAsync(ct));

    [HttpPost("aceptar")]
    public Task<IActionResult> Accept(AcceptPrivacyCommand command, CancellationToken ct) =>
        Run(() => privacy.AcceptAsync(command, ct));

    [HttpGet("datos")]
    public Task<IActionResult> Export(CancellationToken ct) => RunFile(() => privacy.ExportAsync(ct));

    [HttpPost("cerrar-cuenta")]
    public Task<IActionResult> Close(CloseAccountCommand command, CancellationToken ct) =>
        Run(() => privacy.CloseAccountAsync(command, ct));
}
