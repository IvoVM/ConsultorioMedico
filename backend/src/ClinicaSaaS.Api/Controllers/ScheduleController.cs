using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api")]
public class ScheduleController(ScheduleService schedule) : ApiController
{
    [HttpGet("agendas")]
    public Task<IActionResult> Get([FromQuery] Guid professionalId, CancellationToken ct) =>
        Run(() => schedule.GetScheduleAsync(professionalId, ct));

    [HttpPut("agendas")]
    public Task<IActionResult> Save(SaveScheduleCommand command, CancellationToken ct) =>
        Run(() => schedule.SaveScheduleAsync(command, ct));

    [HttpGet("bloqueos")]
    public Task<IActionResult> Blockouts([FromQuery] Guid professionalId, CancellationToken ct) =>
        Run(() => schedule.BlockoutsAsync(professionalId, ct));

    [HttpPost("bloqueos")]
    public Task<IActionResult> CreateBlockout(CreateBlockoutCommand command, CancellationToken ct) =>
        Run(() => schedule.CreateBlockoutAsync(command, ct));

    [HttpDelete("bloqueos/{id:guid}")]
    public Task<IActionResult> DeleteBlockout(Guid id, CancellationToken ct) =>
        Run(() => schedule.DeleteBlockoutAsync(id, ct));

    [AllowAnonymous]
    [HttpGet("disponibilidad")]
    public Task<IActionResult> Availability([FromQuery] Guid locationId, [FromQuery] Guid specialtyId, [FromQuery] Guid? professionalId, [FromQuery] DateOnly date, CancellationToken ct) =>
        Run(() => schedule.AvailabilityAsync(new AvailabilityQuery(locationId, specialtyId, professionalId, date), ct));
}
