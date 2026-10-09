using ClinicaSaaS.Application;
using ClinicaSaaS.Application.Services.MappingExtensions;
using ClinicaSaaS.Application.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/auditoria")]
public class AuditController(IAuditStore audit) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(async () =>
        await audit.Entries.OrderByDescending(a => a.Timestamp).Take(200).ToAuditDtos().ToListAsync(ct));
}
