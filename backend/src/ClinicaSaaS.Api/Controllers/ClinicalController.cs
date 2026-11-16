using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

// The clinical record. Access levels follow Ley 27.706, art. 7: the patient reads their own record; the
// professional reads it, writes and signs new pages and corrects their own. Administration and front desk
// never reach it. Prescriptions are not issued here until the platform is in the national registry.
[ApiController]
[Authorize]
[Route("api")]
public class ClinicalController(ClinicalService clinical) : ApiController
{
    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.TenantAdmin}")]
    [HttpGet("diagnosticos")]
    public Task<IActionResult> Diagnoses([FromQuery] string? q, CancellationToken ct) => Run(() => clinical.DiagnosesAsync(q, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost("diagnosticos/catalogo")]
    public Task<IActionResult> ImportDiagnoses(ImportDiagnosesCommand command, CancellationToken ct) =>
        Run(() => clinical.ImportDiagnosesAsync(command, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpGet("encuentros/turno/{appointmentId:guid}")]
    public Task<IActionResult> EncounterForAppointment(Guid appointmentId, CancellationToken ct) =>
        Run(() => clinical.EncounterForAppointmentAsync(appointmentId, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpGet("encuentros/pendientes")]
    public Task<IActionResult> PendingEncounters(CancellationToken ct) => Run(() => clinical.PendingEncountersAsync(ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("encuentros")]
    public Task<IActionResult> SaveEncounter(SaveEncounterCommand command, CancellationToken ct) =>
        Run(() => clinical.SaveEncounterAsync(command, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("encuentros/{id:guid}/firmar")]
    public Task<IActionResult> SignEncounter(Guid id, SignCommand command, CancellationToken ct) =>
        Run(() => clinical.SignEncounterAsync(id, command, ct));

    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Patient}")]
    [HttpGet("historia")]
    public Task<IActionResult> History([FromQuery] Guid? patientId, [FromQuery] Guid? specialtyId, CancellationToken ct) =>
        Run(() => clinical.HistoryAsync(patientId, specialtyId, ct));

    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Patient}")]
    [HttpGet("historia/copia")]
    public Task<IActionResult> Copy([FromQuery] Guid? patientId, CancellationToken ct) =>
        RunFile(() => clinical.CopyAsync(patientId, ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("historia/accesos")]
    public Task<IActionResult> MyAccesses(CancellationToken ct) => Run(() => clinical.MyAccessesAsync(ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("historia/antecedentes")]
    public Task<IActionResult> AddBackground(AddBackgroundCommand command, CancellationToken ct) =>
        Run(() => clinical.AddBackgroundAsync(command, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("historia/consentimientos")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public Task<IActionResult> AddConsent(AddConsentCommand command, CancellationToken ct) =>
        Run(() => clinical.AddConsentAsync(command, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPost("historia/folios/{id:guid}/correcciones")]
    public Task<IActionResult> Amend(Guid id, AmendEntryCommand command, CancellationToken ct) =>
        Run(() => clinical.AmendAsync(id, command, ct));

    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Patient}")]
    [HttpGet("historia/folios/{id:guid}/adjunto")]
    public Task<IActionResult> Attachment(Guid id, CancellationToken ct) =>
        RunFile(() => clinical.AttachmentAsync(id, ct));

    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Patient}")]
    [HttpGet("historia/folios/{id:guid}/firma")]
    public Task<IActionResult> EntrySignature(Guid id, CancellationToken ct) =>
        RunFile(() => clinical.EntrySignatureAsync(id, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpGet("historia/mi-firma")]
    public Task<IActionResult> MySignature(CancellationToken ct) => Run(() => clinical.MySignatureAsync(ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpGet("historia/mi-firma/imagen")]
    public Task<IActionResult> MySignatureImage(CancellationToken ct) => RunFile(() => clinical.MySignatureImageAsync(ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpPut("historia/mi-firma")]
    [RequestSizeLimit(1024 * 1024)]
    public Task<IActionResult> SaveSignature(SaveSignatureCommand command, CancellationToken ct) =>
        Run(() => clinical.SaveSignatureAsync(command, ct));

    [Authorize(Roles = AppRoles.Doctor)]
    [HttpDelete("historia/mi-firma")]
    public Task<IActionResult> RemoveSignature(CancellationToken ct) => Run(() => clinical.RemoveSignatureAsync(ct));
}
