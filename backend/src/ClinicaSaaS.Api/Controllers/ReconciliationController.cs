using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/conciliacion")]
public class ReconciliationController(ReconciliationService reconciliation) : ApiController
{
    [HttpGet]
    public Task<IActionResult> Get(CancellationToken ct) => Run(() => reconciliation.GetAsync(ct));

    [HttpPost("liquidaciones")]
    public Task<IActionResult> Import(ImportSettlementCommand command, CancellationToken ct) => Run(() => reconciliation.ImportAsync(command, ct));

    [HttpPost("lineas/{lineId:guid}/asociar")]
    public Task<IActionResult> Match(Guid lineId, MatchLineCommand command, CancellationToken ct) => Run(() => reconciliation.MatchAsync(lineId, command, ct));
}
