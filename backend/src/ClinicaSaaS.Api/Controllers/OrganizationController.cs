using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api")]
public class OrganizationController(OrganizationService organization) : ApiController
{
    [AllowAnonymous]
    [HttpGet("sedes")]
    public Task<IActionResult> Locations(CancellationToken ct) => Run(() => organization.LocationsAsync(ct));

    [HttpPost("sedes")]
    public Task<IActionResult> CreateLocation(SaveLocationCommand command, CancellationToken ct) =>
        Run(() => organization.CreateLocationAsync(command, ct));

    [HttpPut("sedes/{id:guid}")]
    public Task<IActionResult> UpdateLocation(Guid id, SaveLocationCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateLocationAsync(id, command, ct));

    [HttpGet("servicios")]
    public Task<IActionResult> MedicalServices(CancellationToken ct) => Run(() => organization.MedicalServicesAsync(ct));

    [HttpPost("servicios")]
    public Task<IActionResult> CreateMedicalService(SaveMedicalServiceCommand command, CancellationToken ct) =>
        Run(() => organization.CreateMedicalServiceAsync(command, ct));

    [HttpPut("servicios/{id:guid}")]
    public Task<IActionResult> UpdateMedicalService(Guid id, SaveMedicalServiceCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateMedicalServiceAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("especialidades")]
    public Task<IActionResult> Specialties(CancellationToken ct) =>
        Run(() => organization.SpecialtiesAsync(ct));

    [HttpPost("especialidades")]
    public Task<IActionResult> CreateSpecialty(SaveSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.CreateSpecialtyAsync(command, ct));

    [HttpPut("especialidades/{id:guid}")]
    public Task<IActionResult> UpdateSpecialty(Guid id, SaveSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateSpecialtyAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("tipos-turno")]
    public Task<IActionResult> AppointmentTypes(CancellationToken ct) => Run(() => organization.AppointmentTypesAsync(ct));

    [HttpPost("tipos-turno")]
    public Task<IActionResult> CreateAppointmentType(SaveAppointmentTypeCommand command, CancellationToken ct) =>
        Run(() => organization.CreateAppointmentTypeAsync(command, ct));

    [HttpPut("tipos-turno/{id:guid}")]
    public Task<IActionResult> UpdateAppointmentType(Guid id, SaveAppointmentTypeCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateAppointmentTypeAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("profesionales")]
    public Task<IActionResult> Professionals([FromQuery] Guid? specialtyId, CancellationToken ct) =>
        Run(() => organization.ProfessionalsAsync(specialtyId, ct));

    [HttpPut("profesionales/{id:guid}/especialidad")]
    public Task<IActionResult> AssignSpecialty(Guid id, AssignSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.AssignSpecialtyAsync(id, command, ct));
}
