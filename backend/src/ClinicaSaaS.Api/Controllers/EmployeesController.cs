using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/empleados")]
public class EmployeesController(EmployeesService employees) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => employees.ListResultAsync(query, ct));

    [HttpPost]
    public Task<IActionResult> Create(CreateEmployeeCommand command, CancellationToken ct) =>
        Run(() => employees.CreateAsync(command, ct));

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, UpdateEmployeeCommand command, CancellationToken ct) =>
        Run(() => employees.UpdateAsync(id, command, ct));

    [HttpPut("{id:guid}/estado")]
    public Task<IActionResult> SetActive(Guid id, SetEmployeeActiveCommand command, CancellationToken ct) =>
        Run(() => employees.SetActiveAsync(id, command.Active, ct));

    [HttpPost("importar")]
    public Task<IActionResult> Import(ImportEmployeesCommand command, CancellationToken ct) =>
        Run(() => employees.ImportAsync(command, ct));
}
