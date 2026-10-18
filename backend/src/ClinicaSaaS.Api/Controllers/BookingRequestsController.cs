using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Route("api/solicitudes")]
public class BookingRequestsController(ScheduleService schedule) : ApiController
{
    [AllowAnonymous]
    [HttpPost]
    public Task<IActionResult> Create(CreateBookingRequestCommand command, CancellationToken ct) =>
        Run(() => schedule.CreateRequestAsync(command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpGet]
    public Task<IActionResult> Pending(CancellationToken ct) =>
        Run(() => schedule.PendingRequestsAsync(ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("mias")]
    public Task<IActionResult> Mine(CancellationToken ct) =>
        Run(() => schedule.MyRequestsAsync(ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/aprobar")]
    public Task<IActionResult> Approve(Guid id, CancellationToken ct) =>
        Run(() => schedule.ApproveRequestAsync(id, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/rechazar")]
    public Task<IActionResult> Reject(Guid id, CancellationToken ct) =>
        Run(() => schedule.RejectRequestAsync(id, ct));
}
