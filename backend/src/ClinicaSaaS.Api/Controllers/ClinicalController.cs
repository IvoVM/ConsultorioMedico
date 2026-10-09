using ClinicaSaaS.Application;
using ClinicaSaaS.Application.Services;
using ClinicaSaaS.Application.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Secretary},{AppRoles.TenantAdmin},{AppRoles.Patient}")]
[Route("api")]
public class ClinicalController(ClinicalService clinical) : ApiController
{
    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpGet("diagnosticos")]
    public Task<IActionResult> Diagnoses(CancellationToken ct) => Run(() => clinical.DiagnosesAsync(ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("encuentros")]
    public Task<IActionResult> SaveEncounter(SaveEncounterCommand command, CancellationToken ct) =>
        Run(() => clinical.SaveEncounterAsync(command, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("encuentros/{id:guid}/cerrar")]
    public Task<IActionResult> CloseEncounter(Guid id, CancellationToken ct) =>
        Run(() => clinical.CloseEncounterAsync(id, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("recetas")]
    public Task<IActionResult> CreatePrescription(CreatePrescriptionCommand command, CancellationToken ct) =>
        Run(() => clinical.CreatePrescriptionAsync(command, ct));

    [HttpGet("recetas/{id:guid}")]
    public Task<IActionResult> GetPrescription(Guid id, CancellationToken ct) =>
        Run(() => clinical.GetPrescriptionAsync(id, ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("recetas/mias")]
    public Task<IActionResult> MyPrescriptions(CancellationToken ct) => Run(() => clinical.MyPrescriptionsAsync(ct));

    [HttpGet("historia")]
    public Task<IActionResult> History([FromQuery] Guid? patientId, CancellationToken ct) =>
        Run(() => clinical.HistoryAsync(patientId, ct));
}
