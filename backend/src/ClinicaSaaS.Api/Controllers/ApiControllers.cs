using ClinicaSaaS.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/salud")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}

[ApiController]
[AllowAnonymous]
[Route("api/clinica")]
public class ClinicController(ICurrentTenant clinic) : ControllerBase
{
    [HttpGet]
    public ClinicDto Get() => new(clinic.Slug, clinic.Name);
}

[ApiController]
[AllowAnonymous]
[Route("api/acceso")]
public class AuthController(AuthService auth) : ApiController
{
    [HttpPost("ingreso")]
    public Task<IActionResult> Login(LoginCommand command, CancellationToken ct) =>
        Run(() => auth.LoginTenantAsync(command, ct));

    [HttpPost("registro")]
    public Task<IActionResult> Register(RegisterPatientCommand command, CancellationToken ct) =>
        Run(() => auth.RegisterPatientAsync(command, ct));
}

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
}

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/empleados")]
public class EmployeesController(EmployeesService employees) : ApiController
{
    [HttpPost("importar")]
    public Task<IActionResult> Import(ImportEmployeesCommand command, CancellationToken ct) =>
        Run(() => employees.ImportAsync(command, ct));
}

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api")]
public class ScheduleController(ScheduleService schedule) : ApiController
{
    [HttpGet("agendas")]
    public Task<IActionResult> Get([FromQuery] Guid professionalId, CancellationToken ct) =>
        Run(() => schedule.GetScheduleAsync(professionalId, ct));

    [HttpPut("agendas")]
    public Task<IActionResult> Save(SaveScheduleCommand command, CancellationToken ct) =>
        Run(() => schedule.SaveScheduleAsync(command, ct));

    [HttpGet("bloqueos")]
    public Task<IActionResult> Blockouts([FromQuery] Guid professionalId, CancellationToken ct) =>
        Run(() => schedule.BlockoutsAsync(professionalId, ct));

    [HttpPost("bloqueos")]
    public Task<IActionResult> CreateBlockout(CreateBlockoutCommand command, CancellationToken ct) =>
        Run(() => schedule.CreateBlockoutAsync(command, ct));

    [HttpDelete("bloqueos/{id:guid}")]
    public Task<IActionResult> DeleteBlockout(Guid id, CancellationToken ct) =>
        Run(() => schedule.DeleteBlockoutAsync(id, ct));

    [AllowAnonymous]
    [HttpGet("disponibilidad")]
    public Task<IActionResult> Availability([FromQuery] Guid locationId, [FromQuery] Guid specialtyId, [FromQuery] Guid? professionalId, [FromQuery] DateOnly date, CancellationToken ct) =>
        Run(() => schedule.AvailabilityAsync(new AvailabilityQuery(locationId, specialtyId, professionalId, date), ct));
}

[ApiController]
[Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin},{AppRoles.Doctor}")]
[Route("api/turnos")]
public class AppointmentsController(ScheduleService schedule) : ApiController
{
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost]
    public Task<IActionResult> Book(BookAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.BookAsync(command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin},{AppRoles.Doctor}")]
    [HttpGet]
    public Task<IActionResult> ForDay([FromQuery] DateOnly date, [FromQuery] Guid? professionalId, CancellationToken ct) =>
        Run(() => schedule.ForDayAsync(date, professionalId, ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("mios")]
    public Task<IActionResult> Mine(CancellationToken ct) => Run(() => schedule.MineAsync(ct));

    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/cancelar")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Run(() => schedule.CancelAsync(id, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/admitir")]
    public Task<IActionResult> CheckIn(Guid id, CancellationToken ct) => Run(() => schedule.CheckInAsync(id, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/reprogramar")]
    public Task<IActionResult> Reschedule(Guid id, RescheduleAppointmentCommand command, CancellationToken ct) =>
        Run(() => schedule.RescheduleAsync(id, command, ct));
}

[ApiController]
[Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
[Route("api/lista-espera")]
public class WaitlistController(ScheduleService schedule) : ApiController
{
    [HttpPost]
    public Task<IActionResult> Create(CreateWaitlistEntryCommand command, CancellationToken ct) =>
        Run(() => schedule.JoinWaitlistAsync(command, ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(() => schedule.WaitlistAsync(ct));

    [Authorize(Roles = $"{AppRoles.Secretary},{AppRoles.TenantAdmin}")]
    [HttpPost("{id:guid}/asignar")]
    public Task<IActionResult> Assign(Guid id, AssignWaitlistEntryCommand command, CancellationToken ct) =>
        Run(() => schedule.AssignWaitlistEntryAsync(id, command, ct));
}

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

[ApiController]
[Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.Secretary}")]
[Route("api")]
public class BillingController(BillingService billing) : ApiController
{
    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpGet("aranceles")]
    public Task<IActionResult> Fees(CancellationToken ct) => Run(() => billing.FeesAsync(ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost("aranceles")]
    public Task<IActionResult> CreateFee(CreateFeeCommand command, CancellationToken ct) =>
        Run(() => billing.CreateFeeAsync(command, ct));

    [HttpGet("comprobantes")]
    public Task<IActionResult> Invoices(CancellationToken ct) => Run(() => billing.InvoicesAsync(ct));

    [HttpPost("comprobantes/{id:guid}/pagar")]
    public Task<IActionResult> Pay(Guid id, PayInvoiceCommand command, CancellationToken ct) =>
        Run(() => billing.PayAsync(id, command, ct));
}

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/auditoria")]
public class AuditController(IAuditStore audit) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(async () =>
        (await audit.ListAsync(ct))
            .Select(a => new AuditEntryDto(a.Id, a.UserId, a.Action, a.Entity, a.EntityId, a.Detail, a.Timestamp))
            .ToList());
}
