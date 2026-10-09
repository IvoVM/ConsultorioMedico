using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin},{AppRoles.Doctor}")]
[Route("api/turnos")]
public class AppointmentsController(ScheduleService schedule) : ApiController
{
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost]
    public Task<IActionResult> Book(BookAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.BookAsync(command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin},{AppRoles.Doctor}")]
    [HttpGet]
    public Task<IActionResult> ForDay([FromQuery] DateOnly date, [FromQuery] Guid? professionalId, CancellationToken ct) =>
        Run(() => schedule.ForDayAsync(date, professionalId, ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("mios")]
    public Task<IActionResult> Mine(CancellationToken ct) => Run(() => schedule.MineAsync(ct));

    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/cancelar")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Run(() => schedule.CancelAsync(id, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/admitir")]
    public Task<IActionResult> CheckIn(Guid id, CancellationToken ct) => Run(() => schedule.CheckInAsync(id, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/reprogramar")]
    public Task<IActionResult> Reschedule(Guid id, RescheduleAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.RescheduleAsync(id, command, ct));
}
