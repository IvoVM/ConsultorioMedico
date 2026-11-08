using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class ClinicalService(
    IClinicalStore clinical,
    IAppointmentStore appointments,
    IClientStore clients,
    IBillingStore billing,
    IOrganizationStore organization,
    IEmployeeStore employees,
    ICurrentUser currentUser,
    IAuditStore audit,
    IValidator<SaveEncounterCommand> encounterValidator,
    IValidator<CreatePrescriptionCommand> prescriptionValidator,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<DiagnosisDto>> DiagnosesAsync(CancellationToken ct) =>
        await clinical.Diagnoses.OrderBy(d => d.Code).ToDiagnosisDtos().ToListAsync(ct);

    public async Task<EncounterDto> SaveEncounterAsync(SaveEncounterCommand command, CancellationToken ct)
    {
        var result = await encounterValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var appointment = await appointments.GetAsync(command.AppointmentId, ct) ?? throw new NotFoundException("Turno no encontrado.");
        if (currentUser.Role == TenantRole.Doctor.ToString() && appointment.ProfessionalId != currentUser.Id)
            throw new BusinessRuleException("Solo el profesional del turno puede cargar la consulta.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Completed)
            throw new BusinessRuleException("Ese turno ya no se puede atender.");

        var encounter = await clinical.GetEncounterByAppointmentAsync(appointment.Id, ct);
        if (encounter is null)
        {
            encounter = new Encounter
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                ClientId = appointment.ClientId,
                ProfessionalId = appointment.ProfessionalId,
                SpecialtyId = await SpecialtyForAsync(appointment, ct),
                CreatedAt = clock.GetUtcNow()
            };
            encounter = await clinical.AddEncounterAsync(encounter, ct);
        }
        else if (encounter.IsClosed)
        {
            throw new BusinessRuleException("Esta consulta ya está cerrada.");
        }

        encounter.Note = command.Note?.Trim();
        encounter.BloodPressure = command.BloodPressure?.Trim();
        encounter.HeartRate = command.HeartRate;
        encounter.Temperature = command.Temperature;
        encounter.WeightKg = command.WeightKg;
        await clinical.ReplaceDiagnosesAsync(encounter.Id, command.DiagnosisIds, ct);
        if (appointment.Status is AppointmentStatus.Booked or AppointmentStatus.Rescheduled or AppointmentStatus.CheckedIn)
            appointment.Status = AppointmentStatus.InProgress;
        await appointments.SaveAsync(ct);
        await clinical.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "encuentro", "Encuentro", encounter.Id.ToString(), null, ct);
        return await LoadEncounterAsync(encounter.Id, ct);
    }

    public async Task<EncounterDto> CloseEncounterAsync(Guid id, CancellationToken ct)
    {
        var encounter = await clinical.GetEncounterAsync(id, ct) ?? throw new NotFoundException("Consulta no encontrada.");
        if (!EncounterRules.CanClose(encounter.Note))
            throw new BusinessRuleException("Para cerrar la consulta hace falta una nota clínica.");
        if (encounter.IsClosed)
            return await LoadEncounterAsync(encounter.Id, ct);

        var appointment = await appointments.GetAsync(encounter.AppointmentId, ct) ?? throw new NotFoundException("Turno no encontrado.");
        encounter.IsClosed = true;
        appointment.Status = AppointmentStatus.Completed;
        await clinical.SaveAsync(ct);
        await appointments.SaveAsync(ct);
        await CreateInvoiceIfMissingAsync(appointment, ct);
        await audit.RecordAsync(currentUser.Id, "cierre", "Encuentro", encounter.Id.ToString(), null, ct);
        return await LoadEncounterAsync(encounter.Id, ct);
    }

    public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionCommand command, CancellationToken ct)
    {
        var result = await prescriptionValidator.ValidateAsync(command, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        var encounter = await clinical.GetEncounterAsync(command.EncounterId, ct) ?? throw new NotFoundException("Consulta no encontrada.");
        if (currentUser.Role == TenantRole.Doctor.ToString() && encounter.ProfessionalId != currentUser.Id)
            throw new BusinessRuleException("Solo el profesional de la consulta puede emitir la receta.");

        var prescription = await clinical.AddPrescriptionAsync(new Prescription
        {
            Id = Guid.NewGuid(),
            EncounterId = encounter.Id,
            ClientId = encounter.ClientId,
            ProfessionalId = encounter.ProfessionalId,
            Instructions = command.Instructions?.Trim(),
            CreatedAt = clock.GetUtcNow(),
            Items = command.Items.Select(i => new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                Medication = i.Medication.Trim(),
                Dose = i.Dose.Trim(),
                Frequency = i.Frequency.Trim(),
                Duration = i.Duration.Trim()
            }).ToList()
        }, ct);
        await audit.RecordAsync(currentUser.Id, "receta", "Receta", prescription.Id.ToString(), null, ct);
        return await clinical.Prescriptions.Where(p => p.Id == prescription.Id).ToPrescriptionDtos().FirstAsync(ct);
    }

    public async Task<PrescriptionDto> GetPrescriptionAsync(Guid id, CancellationToken ct)
    {
        var prescription = await clinical.Prescriptions.Where(p => p.Id == id).ToPrescriptionDtos().FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Receta no encontrada.");
        if (currentUser.Role == TenantRole.Doctor.ToString())
        {
            var own = await OwnSpecialtyAsync(ct);
            Guid? filter = own.Id == Guid.Empty ? null : own.Id;
            if (!await clinical.Prescriptions.AnyAsync(p => p.Id == id && p.Encounter.SpecialtyId == filter, ct))
                throw new BusinessRuleException("Solo podés ver recetas de tu especialidad.");
            if (!await IsOwnPatientAsync(prescription.PatientId, ct))
                throw new BusinessRuleException("Solo podés ver recetas de tus pacientes.");
            return prescription;
        }
        await EnsurePatientAccessAsync(prescription.PatientId, ct);
        return prescription;
    }

    // A patient has one history per specialty. The professional only opens the one of their own specialty;
    // the patient can switch between all of theirs. Guid.Empty is the history of visits without a specialty.
    public async Task<ClinicalHistoryDto> HistoryAsync(Guid? patientId, Guid? specialtyId, CancellationToken ct)
    {
        var clientId = await ResolvePatientAsync(patientId, ct);
        var summary = await clients.Clients.Where(c => c.Id == clientId).ToPatientSummaries().FirstAsync(ct);

        var groups = await clinical.Encounters
            .Where(e => e.ClientId == clientId)
            .GroupBy(e => e.SpecialtyId)
            .Select(g => new { SpecialtyId = g.Key, Count = g.Count(), Last = g.Max(e => e.CreatedAt) })
            .ToListAsync(ct);
        var ids = groups.Where(g => g.SpecialtyId != null).Select(g => g.SpecialtyId!.Value).ToList();
        var names = await organization.Specialties
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var available = groups
            .OrderByDescending(g => g.Last)
            .Select(g => new HistorySpecialtyDto(
                g.SpecialtyId ?? Guid.Empty,
                g.SpecialtyId is Guid id && names.TryGetValue(id, out var name) ? name : NoSpecialty,
                g.Count))
            .ToList();

        HistorySpecialtyDto? chosen;
        if (currentUser.Role == TenantRole.Doctor.ToString())
        {
            var own = await OwnSpecialtyAsync(ct);
            if (specialtyId is Guid asked && asked != own.Id)
                throw new BusinessRuleException("Solo podés ver la historia de tu especialidad.");
            chosen = available.FirstOrDefault(s => s.Id == own.Id) ?? own;
            available = [chosen];
        }
        else
        {
            chosen = specialtyId is Guid asked
                ? available.FirstOrDefault(s => s.Id == asked) ?? throw new NotFoundException("No hay historia en esa especialidad.")
                : available.FirstOrDefault();
        }

        if (chosen is null)
            return new ClinicalHistoryDto(summary, null, available, [], []);

        Guid? filter = chosen.Id == Guid.Empty ? null : chosen.Id;
        var encounters = await clinical.Encounters
            .Where(e => e.ClientId == clientId && e.SpecialtyId == filter)
            .OrderByDescending(e => e.CreatedAt)
            .ToEncounterDtos()
            .ToListAsync(ct);
        var prescriptions = await clinical.Prescriptions
            .Where(p => p.ClientId == clientId && p.Encounter.SpecialtyId == filter)
            .OrderByDescending(p => p.CreatedAt)
            .ToPrescriptionDtos()
            .ToListAsync(ct);
        return new ClinicalHistoryDto(summary, chosen, available, encounters, prescriptions);
    }

    public async Task<IReadOnlyList<PrescriptionDto>> MyPrescriptionsAsync(CancellationToken ct)
    {
        var clientId = await ResolvePatientAsync(null, ct);
        return await clinical.Prescriptions
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToPrescriptionDtos()
            .ToListAsync(ct);
    }

    private const string NoSpecialty = "Sin especialidad";

    private async Task<Guid> ResolvePatientAsync(Guid? patientId, CancellationToken ct)
    {
        if (currentUser.Role == TenantRole.Patient.ToString())
        {
            var own = await clients.Clients
                .Where(c => c.UserId == (currentUser.Id ?? Guid.Empty))
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);
            return own == Guid.Empty ? throw new NotFoundException("No hay un paciente asociado a esta cuenta.") : own;
        }
        if (currentUser.Role != TenantRole.Doctor.ToString())
            throw new BusinessRuleException("Solo el profesional puede ver la historia clínica.");
        if (patientId is not Guid clientId)
            throw new BusinessRuleException("Indicá el paciente.");
        if (!await clients.Clients.AnyAsync(c => c.Id == clientId, ct))
            throw new NotFoundException("Paciente no encontrado.");
        if (!await IsOwnPatientAsync(clientId, ct))
            throw new BusinessRuleException("Solo podés ver la historia de tus pacientes.");
        return clientId;
    }

    // A patient belongs to a professional once they attended them or hold a live appointment with them.
    // An appointment moved to someone else leaves with the move, so the first professional never "knew" the patient.
    private async Task<bool> IsOwnPatientAsync(Guid clientId, CancellationToken ct)
    {
        var me = currentUser.Id ?? Guid.Empty;
        return await clinical.Encounters.AnyAsync(e => e.ClientId == clientId && e.ProfessionalId == me, ct)
            || await appointments.Appointments.AnyAsync(a => a.ClientId == clientId && a.ProfessionalId == me && a.Status != AppointmentStatus.Cancelled, ct);
    }

    private async Task<HistorySpecialtyDto> OwnSpecialtyAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? Guid.Empty;
        var own = await employees.Employees
            .Where(e => e.UserId == userId && e.SpecialtyId != null)
            .Select(e => new { Id = e.SpecialtyId!.Value, e.Specialty!.Name })
            .FirstOrDefaultAsync(ct);
        return own is null ? new HistorySpecialtyDto(Guid.Empty, NoSpecialty, 0) : new HistorySpecialtyDto(own.Id, own.Name, 0);
    }

    private async Task<Guid?> SpecialtyForAsync(Appointment appointment, CancellationToken ct) =>
        await organization.AppointmentTypes
            .Where(t => t.Id == appointment.AppointmentTypeId)
            .Select(t => t.SpecialtyId)
            .FirstOrDefaultAsync(ct)
        ?? await employees.Employees
            .Where(e => e.UserId == appointment.ProfessionalId)
            .Select(e => e.SpecialtyId)
            .FirstOrDefaultAsync(ct);

    private async Task CreateInvoiceIfMissingAsync(Appointment appointment, CancellationToken ct)
    {
        if (await billing.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id, ct))
            return;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        string? insurance = null;
        if (appointment.InsurerId is Guid insurerId && appointment.CoverageAccepted != false)
            insurance = await organization.Insurers.Where(i => i.Id == insurerId).Select(i => i.Name).FirstOrDefaultAsync(ct);
        var fees = await billing.Fees
            .Where(f => f.AppointmentTypeId == appointment.AppointmentTypeId && f.EffectiveFrom <= today)
            .Select(f => new { f.Payer, f.Amount, f.EffectiveFrom, f.Id, f.AppointmentType.Name })
            .ToListAsync(ct);
        var covered = fees.Where(f => SamePayer(f.Payer, insurance)).ToList();
        if (covered.Count == 0)
            covered = fees.Where(f => BillingRules.CanonicalPayer(f.Payer) is null).ToList();
        var chosen = covered.OrderByDescending(f => f.EffectiveFrom).ThenByDescending(f => f.Id).FirstOrDefault();
        var amount = chosen?.Amount ?? 0;
        await billing.AddInvoiceAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            ClientId = appointment.ClientId,
            Total = BillingRules.Total([amount]),
            Status = InvoiceStatus.Pending,
            CreatedAt = clock.GetUtcNow(),
            Items =
            [
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = chosen?.Name ?? "Consulta",
                    Amount = amount
                }
            ]
        }, ct);
    }

    private static bool SamePayer(string? feePayer, string? insurance) =>
        string.Equals(BillingRules.CanonicalPayer(feePayer), BillingRules.CanonicalPayer(insurance), StringComparison.OrdinalIgnoreCase);

    private async Task EnsurePatientAccessAsync(Guid patientId, CancellationToken ct)
    {
        if (currentUser.Role != TenantRole.Patient.ToString())
            throw new BusinessRuleException("Solo el profesional puede ver la historia clínica.");
        var clientId = await clients.Clients
            .Where(c => c.UserId == (currentUser.Id ?? Guid.Empty))
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
        if (clientId != patientId)
            throw new BusinessRuleException("No podés ver datos de otra persona.");
    }

    private Task<EncounterDto> LoadEncounterAsync(Guid id, CancellationToken ct) =>
        clinical.Encounters.Where(e => e.Id == id).ToEncounterDtos().FirstAsync(ct);
}
