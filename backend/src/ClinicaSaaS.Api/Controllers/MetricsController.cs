using ClinicaSaaS.Domain;
using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/metricas")]
public class MetricsController(MetricsService metrics) : ApiController
{
    [HttpGet]
    public Task<IActionResult> Get([FromQuery] MetricsRange range = MetricsRange.Month, CancellationToken ct = default) =>
        Run(() => metrics.MetricsAsync(range, ct));
}
