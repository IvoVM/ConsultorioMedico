using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Route("api/agenda")]
public class AgendaController(ScheduleService schedule) : ApiController
{
    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin},{AppRoles.Doctor}")]
    [HttpGet]
    public Task<IActionResult> Day(DateOnly date, CancellationToken ct) =>
        Run(() => schedule.AgendaAsync(date, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("turnos")]
    public Task<IActionResult> Book(AgendaBookCommand command, CancellationToken ct) =>
        Run(() => schedule.BookOnAgendaAsync(command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("turnos/{id:guid}/mover")]
    public Task<IActionResult> Move(Guid id, MoveAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.MoveOnAgendaAsync(id, command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPut("turnos/{id:guid}")]
    public Task<IActionResult> Edit(Guid id, EditAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.EditOnAgendaAsync(id, command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("solicitudes/{id:guid}/ubicar")]
    public Task<IActionResult> PlaceRequest(Guid id, PlaceRequestCommand command, CancellationToken ct) =>
        Run(() => schedule.PlaceRequestOnAgendaAsync(id, command, ct));
}
