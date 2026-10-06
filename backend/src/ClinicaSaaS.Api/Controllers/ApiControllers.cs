using ClinicaSaaS.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Route("api/salud")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}

[ApiController]
[Route("api/plataforma/acceso")]
public class PlatformAuthController(PlatformAuthService auth) : ApiController
{
    [HttpPost("ingreso")]
    public Task<IActionResult> Login(LoginCommand command, CancellationToken ct) =>
        Run(() => auth.LoginAsync(command, ct));
}

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/plataforma/consultorios")]
public class PlatformTenantsController(TenantsService tenants) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(() => tenants.ListAsync(ct));

    [HttpPost]
    public Task<IActionResult> Create(CreateTenantCommand command, CancellationToken ct) =>
        Run(() => tenants.CreateAsync(command, ct));

    [HttpPatch("{id:guid}/estado")]
    public Task<IActionResult> ChangeStatus(Guid id, ChangeTenantStatusCommand command, CancellationToken ct) =>
        Run(() => tenants.ChangeStatusAsync(id, command, ct));
}

[ApiController]
[Route("api/acceso")]
public class AuthController(AuthService auth) : ApiController
{
    [AllowAnonymous]
    [HttpPost("ingreso")]
    public Task<IActionResult> Login(LoginCommand command, CancellationToken ct) =>
        Run(() => auth.LoginTenantAsync(command, ct));

    [AllowAnonymous]
    [HttpPost("registro")]
    public Task<IActionResult> Register(RegisterPatientCommand command, CancellationToken ct) =>
        Run(() => auth.RegisterPatientAsync(command, ct));
}

[ApiController]
[Authorize]
[Route("api")]
public class OrganizationController(OrganizationService organization) : ApiController
{
    [AllowAnonymous]
    [HttpGet("sedes")]
    public Task<IActionResult> Locations(CancellationToken ct) => Run(() => organization.LocationsAsync(ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost("sedes")]
    public Task<IActionResult> CreateLocation(SaveLocationCommand command, CancellationToken ct) =>
        Run(() => organization.CreateLocationAsync(command, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPut("sedes/{id:guid}")]
    public Task<IActionResult> UpdateLocation(Guid id, SaveLocationCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateLocationAsync(id, command, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpGet("servicios")]
    public Task<IActionResult> MedicalServices(CancellationToken ct) => Run(() => organization.MedicalServicesAsync(ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost("servicios")]
    public Task<IActionResult> CreateMedicalService(SaveMedicalServiceCommand command, CancellationToken ct) =>
        Run(() => organization.CreateMedicalServiceAsync(command, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPut("servicios/{id:guid}")]
    public Task<IActionResult> UpdateMedicalService(Guid id, SaveMedicalServiceCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateMedicalServiceAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("especialidades")]
    public Task<IActionResult> Specialties(CancellationToken ct) =>
        Run(() => organization.SpecialtiesAsync(ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost("especialidades")]
    public Task<IActionResult> CreateSpecialty(SaveSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.CreateSpecialtyAsync(command, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPut("especialidades/{id:guid}")]
    public Task<IActionResult> UpdateSpecialty(Guid id, SaveSpecialtyCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateSpecialtyAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("tipos-turno")]
    public Task<IActionResult> AppointmentTypes(CancellationToken ct) => Run(() => organization.AppointmentTypesAsync(ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost("tipos-turno")]
    public Task<IActionResult> CreateAppointmentType(SaveAppointmentTypeCommand command, CancellationToken ct) =>
        Run(() => organization.CreateAppointmentTypeAsync(command, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPut("tipos-turno/{id:guid}")]
    public Task<IActionResult> UpdateAppointmentType(Guid id, SaveAppointmentTypeCommand command, CancellationToken ct) =>
        Run(() => organization.UpdateAppointmentTypeAsync(id, command, ct));

    [AllowAnonymous]
    [HttpGet("profesionales")]
    public Task<IActionResult> Professionals([FromQuery] Guid? specialtyId, CancellationToken ct) =>
        Run(() => organization.ProfessionalsAsync(specialtyId, ct));
}

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[Route("api/empleados")]
public class EmployeesController(EmployeesService employees) : ApiController
{
    [HttpPost("importar")]
    public Task<IActionResult> Import(ImportEmployeesCommand command, CancellationToken ct) =>
        Run(() => employees.ImportAsync(command, ct));
}

[ApiController]
[Authorize]
[Route("api")]
public class ScheduleController(ScheduleService schedule) : ApiController
{
    [Authorize(Roles = "TenantAdmin")]
    [HttpGet("agendas")]
    public Task<IActionResult> Get([FromQuery] Guid professionalId, CancellationToken ct) =>
        Run(() => schedule.GetScheduleAsync(professionalId, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPut("agendas")]
    public Task<IActionResult> Save(SaveScheduleCommand command, CancellationToken ct) =>
        Run(() => schedule.SaveScheduleAsync(command, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpGet("bloqueos")]
    public Task<IActionResult> Blockouts([FromQuery] Guid professionalId, CancellationToken ct) =>
        Run(() => schedule.BlockoutsAsync(professionalId, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost("bloqueos")]
    public Task<IActionResult> CreateBlockout(CreateBlockoutCommand command, CancellationToken ct) =>
        Run(() => schedule.CreateBlockoutAsync(command, ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpDelete("bloqueos/{id:guid}")]
    public Task<IActionResult> DeleteBlockout(Guid id, CancellationToken ct) =>
        Run(() => schedule.DeleteBlockoutAsync(id, ct));

    [AllowAnonymous]
    [HttpGet("disponibilidad")]
    public Task<IActionResult> Availability([FromQuery] Guid locationId, [FromQuery] Guid specialtyId, [FromQuery] Guid? professionalId, [FromQuery] DateOnly date, CancellationToken ct) =>
        Run(() => schedule.AvailabilityAsync(new AvailabilityQuery(locationId, specialtyId, professionalId, date), ct));
}

[ApiController]
[Authorize]
[Route("api/turnos")]
public class AppointmentsController(ScheduleService schedule) : ApiController
{
    [Authorize(Roles = "Patient,Secretary,TenantAdmin")]
    [HttpPost]
    public Task<IActionResult> Book(BookAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.BookAsync(command, ct));

    [Authorize(Roles = "Secretary,TenantAdmin,Doctor")]
    [HttpGet]
    public Task<IActionResult> ForDay([FromQuery] DateOnly date, [FromQuery] Guid? professionalId, CancellationToken ct) =>
        Run(() => schedule.ForDayAsync(date, professionalId, ct));

    [Authorize(Roles = "Patient")]
    [HttpGet("mios")]
    public Task<IActionResult> Mine(CancellationToken ct) => Run(() => schedule.MineAsync(ct));

    [Authorize(Roles = "Patient,Secretary,TenantAdmin")]
    [HttpPost("{id:guid}/cancelar")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Run(() => schedule.CancelAsync(id, ct));

    [Authorize(Roles = "Secretary,TenantAdmin")]
    [HttpPost("{id:guid}/admitir")]
    public Task<IActionResult> CheckIn(Guid id, CancellationToken ct) => Run(() => schedule.CheckInAsync(id, ct));

    [Authorize(Roles = "Secretary,TenantAdmin")]
    [HttpPost("{id:guid}/reprogramar")]
    public Task<IActionResult> Reschedule(Guid id, RescheduleAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.RescheduleAsync(id, command, ct));
}

[ApiController]
[Authorize]
[Route("api/lista-espera")]
public class WaitlistController(ScheduleService schedule) : ApiController
{
    [Authorize(Roles = "Patient,Secretary,TenantAdmin")]
    [HttpPost]
    public Task<IActionResult> Create(CreateWaitlistEntryCommand command, CancellationToken ct) =>
        Run(() => schedule.JoinWaitlistAsync(command, ct));

    [Authorize(Roles = "Secretary,TenantAdmin")]
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(() => schedule.WaitlistAsync(ct));

    [Authorize(Roles = "Secretary,TenantAdmin")]
    [HttpPost("{id:guid}/asignar")]
    public Task<IActionResult> Assign(Guid id, AssignWaitlistEntryCommand command, CancellationToken ct) =>
        Run(() => schedule.AssignWaitlistEntryAsync(id, command, ct));
}

[ApiController]
[Authorize]
[Route("api")]
public class ClinicalController(ClinicalService clinical) : ApiController
{
    [Authorize(Roles = "Doctor,Secretary,TenantAdmin")]
    [HttpGet("diagnosticos")]
    public Task<IActionResult> Diagnoses(CancellationToken ct) => Run(() => clinical.DiagnosesAsync(ct));

    [Authorize(Roles = "Doctor")]
    [HttpPost("encuentros")]
    public Task<IActionResult> SaveEncounter(SaveEncounterCommand command, CancellationToken ct) =>
        Run(() => clinical.SaveEncounterAsync(command, ct));

    [Authorize(Roles = "Doctor")]
    [HttpPost("encuentros/{id:guid}/cerrar")]
    public Task<IActionResult> CloseEncounter(Guid id, CancellationToken ct) =>
        Run(() => clinical.CloseEncounterAsync(id, ct));

    [Authorize(Roles = "Doctor")]
    [HttpPost("recetas")]
    public Task<IActionResult> CreatePrescription(CreatePrescriptionCommand command, CancellationToken ct) =>
        Run(() => clinical.CreatePrescriptionAsync(command, ct));

    [Authorize(Roles = "Doctor,Patient,Secretary,TenantAdmin")]
    [HttpGet("recetas/{id:guid}")]
    public Task<IActionResult> GetPrescription(Guid id, CancellationToken ct) =>
        Run(() => clinical.GetPrescriptionAsync(id, ct));

    [Authorize(Roles = "Patient")]
    [HttpGet("recetas/mias")]
    public Task<IActionResult> MyPrescriptions(CancellationToken ct) => Run(() => clinical.MyPrescriptionsAsync(ct));

    [Authorize(Roles = "Patient,Doctor,Secretary,TenantAdmin")]
    [HttpGet("historia")]
    public Task<IActionResult> History([FromQuery] Guid? patientId, CancellationToken ct) =>
        Run(() => clinical.HistoryAsync(patientId, ct));
}

[ApiController]
[Authorize(Roles = "TenantAdmin")]
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

[ApiController]
[Authorize]
[Route("api")]
public class BillingController(BillingService billing) : ApiController
{
    [Authorize(Roles = "TenantAdmin")]
    [HttpGet("aranceles")]
    public Task<IActionResult> Fees(CancellationToken ct) => Run(() => billing.FeesAsync(ct));

    [Authorize(Roles = "TenantAdmin")]
    [HttpPost("aranceles")]
    public Task<IActionResult> CreateFee(CreateFeeCommand command, CancellationToken ct) =>
        Run(() => billing.CreateFeeAsync(command, ct));

    [Authorize(Roles = "TenantAdmin,Secretary")]
    [HttpGet("comprobantes")]
    public Task<IActionResult> Invoices(CancellationToken ct) => Run(() => billing.InvoicesAsync(ct));

    [Authorize(Roles = "TenantAdmin,Secretary")]
    [HttpPost("comprobantes/{id:guid}/pagar")]
    public Task<IActionResult> Pay(Guid id, PayInvoiceCommand command, CancellationToken ct) =>
        Run(() => billing.PayAsync(id, command, ct));
}

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[Route("api/auditoria")]
public class AuditController(IAuditStore audit) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(async () =>
        (await audit.ListAsync(ct))
            .Select(a => new AuditEntryDto(a.Id, a.UserId, a.Action, a.Entity, a.EntityId, a.Detail, a.Timestamp))
            .ToList());
}
