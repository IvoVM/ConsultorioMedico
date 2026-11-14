using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.Secretary}")]
[Route("api/cajas")]
public class CashController(CashService cash) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List([FromQuery] Guid? locationId, CancellationToken ct) => Run(() => cash.ListAsync(locationId, ct));

    [HttpGet("abiertas")]
    public Task<IActionResult> Open(CancellationToken ct) => Run(() => cash.OpenSessionsAsync(ct));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) => Run(() => cash.GetAsync(id, ct));

    [HttpPost]
    public Task<IActionResult> OpenCash(OpenCashCommand command, CancellationToken ct) => Run(() => cash.OpenAsync(command, ct));

    [HttpPost("{id:guid}/movimientos")]
    public Task<IActionResult> Movement(Guid id, CashMovementCommand command, CancellationToken ct) => Run(() => cash.AddMovementAsync(id, command, ct));

    [HttpPost("{id:guid}/cerrar")]
    public Task<IActionResult> Close(Guid id, CloseCashCommand command, CancellationToken ct) => Run(() => cash.CloseAsync(id, command, ct));
}
