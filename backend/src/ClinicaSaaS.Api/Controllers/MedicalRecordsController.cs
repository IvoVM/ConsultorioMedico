using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/historias-medicas")]
public class MedicalRecordsController(MedicalRecordsService records) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(() => records.ListAsync(ct));

    [HttpGet("{patientId:guid}")]
    public Task<IActionResult> Get(Guid patientId, CancellationToken ct) =>
        Run(() => records.GetAsync(patientId, ct));

    [HttpPut("{patientId:guid}")]
    public Task<IActionResult> Save(Guid patientId, SaveMedicalRecordCommand command, CancellationToken ct) =>
        Run(() => records.SaveAsync(patientId, command, ct));
}
