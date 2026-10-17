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

    [HttpPost("importar")]
    public Task<IActionResult> Import(ImportEmployeesCommand command, CancellationToken ct) =>
        Run(() => employees.ImportAsync(command, ct));
}
