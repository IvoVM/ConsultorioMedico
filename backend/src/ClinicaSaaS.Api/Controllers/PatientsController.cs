using ClinicaSaaS.Application;
using ClinicaSaaS.Application.Services;
using ClinicaSaaS.Application.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.Secretary}")]
[Route("api/pacientes")]
public class PatientsController(MedicalRecordsService records) : ApiController
{
    [HttpGet]
    public Task<IActionResult> Search([FromQuery] string q, CancellationToken ct) =>
        Run(() => records.SearchAsync(q, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost]
    public Task<IActionResult> Create(CreatePatientCommand command, CancellationToken ct) =>
        Run(() => records.CreateAsync(command, ct));
}
