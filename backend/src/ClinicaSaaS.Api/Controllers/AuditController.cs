using ClinicaSaaS.Services;
using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
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
    public Task<IActionResult> List([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(async () =>
        {
            if (!query.Paged)
                return (object)await audit.Entries.OrderByDescending(a => a.Timestamp).Take(200).ToAuditDtos().ToListAsync(ct);
            var source = audit.Entries.AsQueryable();
            if (query.Term is string term)
                source = source.Where(a =>
                    a.Action.ToLower().Contains(term) ||
                    a.Entity.ToLower().Contains(term) ||
                    (a.Detail ?? "").ToLower().Contains(term));
            var ordered = (query.Sort, query.Desc) switch
            {
                ("action", false) => source.OrderBy(a => a.Action).ThenBy(a => a.Timestamp),
                ("action", true) => source.OrderByDescending(a => a.Action).ThenByDescending(a => a.Timestamp),
                ("entity", false) => source.OrderBy(a => a.Entity).ThenBy(a => a.Timestamp),
                ("entity", true) => source.OrderByDescending(a => a.Entity).ThenByDescending(a => a.Timestamp),
                ("detail", false) => source.OrderBy(a => a.Detail ?? "").ThenBy(a => a.Timestamp),
                ("detail", true) => source.OrderByDescending(a => a.Detail ?? "").ThenByDescending(a => a.Timestamp),
                ("date", true) => source.OrderByDescending(a => a.Timestamp),
                _ => source.OrderBy(a => a.Timestamp)
            };
            return (object)await Pages.Materialize(ordered.ToAuditDtos(), query, ct);
        });
}
