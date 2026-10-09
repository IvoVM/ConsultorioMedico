using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
[Route("api/lista-espera")]
public class WaitlistController(ScheduleService schedule) : ApiController
{
    [HttpPost]
    public Task<IActionResult> Create(CreateWaitlistEntryCommand command, CancellationToken ct) =>
        Run(() => schedule.JoinWaitlistAsync(command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(() => schedule.WaitlistAsync(ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/asignar")]
    public Task<IActionResult> Assign(Guid id, AssignWaitlistEntryCommand command, CancellationToken ct) =>
        Run(() => schedule.AssignWaitlistEntryAsync(id, command, ct));
}
