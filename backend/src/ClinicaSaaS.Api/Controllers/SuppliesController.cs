using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

// Front desk sees the stock and loads what comes in; the catalog and the corrections after counting stay with the admin.
[ApiController]
[Route("api/insumos")]
public class SuppliesController(SupplyService supplies) : ApiController
{
    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpGet]
    public Task<IActionResult> List([FromQuery] ListQuery query, [FromQuery] bool low, CancellationToken ct) =>
        Run(() => supplies.ListAsync(query, low, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost]
    public Task<IActionResult> Create(SaveSupplyCommand command, CancellationToken ct) =>
        Run(() => supplies.CreateAsync(command, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, SaveSupplyCommand command, CancellationToken ct) =>
        Run(() => supplies.UpdateAsync(id, command, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPut("{id:guid}/estado")]
    public Task<IActionResult> SetActive(Guid id, SetResourceActiveCommand command, CancellationToken ct) =>
        Run(() => supplies.SetActiveAsync(id, command.Active, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/ingresos")]
    public Task<IActionResult> Entry(Guid id, SupplyEntryCommand command, CancellationToken ct) =>
        Run(() => supplies.EntryAsync(id, command, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost("{id:guid}/conteo")]
    public Task<IActionResult> Count(Guid id, SupplyCountCommand command, CancellationToken ct) =>
        Run(() => supplies.CountAsync(id, command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpGet("{id:guid}/movimientos")]
    public Task<IActionResult> Movements(Guid id, [FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => supplies.MovementsAsync(id, query, ct));
}
