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
    public Task<IActionResult> MedicalServices([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => organization.ListMedicalServicesAsync(query, ct));

    [HttpPost("servicios")]
    public Task<IActionResult> CreateMedicalService(SaveMedicalServiceCommand command, CancellationToken ct) =>
        Run(() => organization.CreateMedicalServiceAsync(command, ct));

    [HttpPut("servicios/{id:guid}")]
    public Task<IActionResult> UpdateMedicalService(Guid id, SaveMedicalServiceCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateMedicalServiceAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("especialidades")]
    public Task<IActionResult> Specialties([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => organization.ListSpecialtiesAsync(query, ct));

    [HttpPost("especialidades")]
    public Task<IActionResult> CreateSpecialty(SaveSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.CreateSpecialtyAsync(command, ct));

    [HttpPut("especialidades/{id:guid}")]
    public Task<IActionResult> UpdateSpecialty(Guid id, SaveSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateSpecialtyAsync(id, command, ct));

    [HttpGet("especialidades/{id:guid}/prestaciones")]
    public Task<IActionResult> Prestaciones(Guid id, CancellationToken ct) =>
        Run(() => organization.PrestacionesAsync(id, ct));

    [AllowAnonymous]
    [HttpGet("tipos-turno")]
    public Task<IActionResult> AppointmentTypes([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => organization.ListAppointmentTypesAsync(query, ct));

    [HttpPost("tipos-turno")]
    public Task<IActionResult> CreateAppointmentType(SaveAppointmentTypeCommand command, CancellationToken ct) =>
        Run(() => organization.CreateAppointmentTypeAsync(command, ct));

    [HttpPut("tipos-turno/{id:guid}")]
    public Task<IActionResult> UpdateAppointmentType(Guid id, SaveAppointmentTypeCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateAppointmentTypeAsync(id, command, ct));

    [HttpPut("tipos-turno/{id:guid}/practica")]
    public Task<IActionResult> SavePractice(Guid id, SavePracticeCommand command, CancellationToken ct) =>
        Run(() => organization.SavePracticeAsync(id, command, ct));

    [HttpGet("consultorios")]
    public Task<IActionResult> Rooms(CancellationToken ct) => Run(() => organization.RoomsAsync(ct));

    [HttpPost("consultorios")]
    public Task<IActionResult> CreateRoom(SaveResourceCommand command, CancellationToken ct) =>
        Run(() => organization.CreateRoomAsync(command, ct));

    [HttpPut("consultorios/{id:guid}")]
    public Task<IActionResult> UpdateRoom(Guid id, SaveResourceCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateRoomAsync(id, command, ct));

    [HttpPut("consultorios/{id:guid}/estado")]
    public Task<IActionResult> SetRoomActive(Guid id, SetResourceActiveCommand command, CancellationToken ct) =>
        Run(() => organization.SetRoomActiveAsync(id, command.Active, ct));

    [HttpGet("equipos")]
    public Task<IActionResult> Equipment(CancellationToken ct) => Run(() => organization.EquipmentAsync(ct));

    [HttpPost("equipos")]
    public Task<IActionResult> CreateEquipment(SaveResourceCommand command, CancellationToken ct) =>
        Run(() => organization.CreateEquipmentAsync(command, ct));

    [HttpPut("equipos/{id:guid}")]
    public Task<IActionResult> UpdateEquipment(Guid id, SaveResourceCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateEquipmentAsync(id, command, ct));

    [HttpPut("equipos/{id:guid}/estado")]
    public Task<IActionResult> SetEquipmentActive(Guid id, SetResourceActiveCommand command, CancellationToken ct) =>
        Run(() => organization.SetEquipmentActiveAsync(id, command.Active, ct));

    [AllowAnonymous]
    [HttpGet("profesionales")]
    public Task<IActionResult> Professionals([FromQuery] Guid? specialtyId, CancellationToken ct) =>
        Run(() => organization.ProfessionalsAsync(specialtyId, ct));

    [HttpPut("profesionales/{id:guid}/especialidad")]
    public Task<IActionResult> AssignSpecialty(Guid id, AssignSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.AssignSpecialtyAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("profesionales/{id:guid}/obras-sociales")]
    public Task<IActionResult> ProfessionalInsurers(Guid id, CancellationToken ct) =>
        Run(() => organization.ProfessionalInsurersAsync(id, ct));

    [HttpGet("obras-sociales")]
    public Task<IActionResult> Insurers([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => organization.ListInsurersAsync(query, ct));

    [HttpGet("obras-sociales/permitidas")]
    public Task<IActionResult> EnabledInsurers(CancellationToken ct) =>
        Run(() => organization.EnabledInsurersAsync(ct));

    [HttpPost("obras-sociales")]
    public Task<IActionResult> CreateInsurer(SaveInsurerCommand command, CancellationToken ct) =>
        Run(() => organization.CreateInsurerAsync(command, ct));

    [HttpPut("obras-sociales/{id:guid}/estado")]
    public Task<IActionResult> SetInsurerEnabled(Guid id, SetInsurerEnabledCommand command, CancellationToken ct) =>
        Run(() => organization.SetInsurerEnabledAsync(id, command.Enabled, ct));
}
