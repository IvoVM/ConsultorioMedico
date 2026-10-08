using ClinicaSaaS.Application.Mappings;
using ClinicaSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Application;

public class ClinicalService(
    IClinicalStore clinical,
    IAppointmentStore appointments,
    IClientStore clients,
    IBillingStore billing,
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
            throw new BusinessRuleException("Solo el profesional del turno puede cargar el encuentro.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Completed)
            throw new BusinessRuleException("Ese turno no admite un encuentro.");

        var encounter = await clinical.GetEncounterByAppointmentAsync(appointment.Id, ct);
        if (encounter is null)
        {
            encounter = new Encounter
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                ClientId = appointment.ClientId,
                ProfessionalId = appointment.ProfessionalId,
                CreatedAt = clock.GetUtcNow()
            };
            encounter = await clinical.AddEncounterAsync(encounter, ct);
        }
        else if (encounter.IsClosed)
        {
            throw new BusinessRuleException("El encuentro ya está cerrado.");
        }

        encounter.Note = command.Note?.Trim();
        encounter.BloodPressure = command.BloodPressure?.Trim();
        encounter.HeartRate = command.HeartRate;
        encounter.Temperature = command.Temperature;
        encounter.WeightKg = command.WeightKg;
        await clinical.ReplaceDiagnosesAsync(encounter.Id, command.DiagnosisIds, ct);
        if (appointment.Status is AppointmentStatus.Booked or AppointmentStatus.CheckedIn)
            appointment.Status = AppointmentStatus.InProgress;
        await appointments.SaveAsync(ct);
        await clinical.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "encuentro", "Encuentro", encounter.Id.ToString(), null, ct);
        return await LoadEncounterAsync(encounter.Id, ct);
    }

    public async Task<EncounterDto> CloseEncounterAsync(Guid id, CancellationToken ct)
    {
        var encounter = await clinical.GetEncounterAsync(id, ct) ?? throw new NotFoundException("Encuentro no encontrado.");
        if (!EncounterRules.CanClose(encounter.Note))
            throw new BusinessRuleException("Para cerrar el encuentro hace falta una nota clínica.");
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
        var encounter = await clinical.GetEncounterAsync(command.EncounterId, ct) ?? throw new NotFoundException("Encuentro no encontrado.");
        if (currentUser.Role == TenantRole.Doctor.ToString() && encounter.ProfessionalId != currentUser.Id)
            throw new BusinessRuleException("Solo el profesional del encuentro puede emitir la receta.");

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
        await EnsurePatientAccessAsync(prescription.PatientId, ct);
        return prescription;
    }

    public async Task<ClinicalHistoryDto> HistoryAsync(Guid? patientId, CancellationToken ct)
    {
        Guid clientId;
        if (currentUser.Role == TenantRole.Patient.ToString())
        {
            clientId = await clients.Clients
                .Where(c => c.UserId == (currentUser.Id ?? Guid.Empty))
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);
            if (clientId == Guid.Empty)
                throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
        }
        else
        {
            if (patientId is null)
                throw new BusinessRuleException("Indicá el paciente.");
            clientId = patientId.Value;
            if (!await clients.Clients.AnyAsync(c => c.Id == clientId, ct))
                throw new NotFoundException("Paciente no encontrado.");
        }

        var summary = await clients.Clients.Where(c => c.Id == clientId).ToPatientSummaries().FirstAsync(ct);
        var encounters = await clinical.Encounters
            .Where(e => e.ClientId == clientId)
            .OrderByDescending(e => e.CreatedAt)
            .ToEncounterDtos()
            .ToListAsync(ct);
        var prescriptions = await clinical.Prescriptions
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToPrescriptionDtos()
            .ToListAsync(ct);
        return new ClinicalHistoryDto(summary, encounters, prescriptions);
    }

    public async Task<IReadOnlyList<PrescriptionDto>> MyPrescriptionsAsync(CancellationToken ct)
    {
        var history = await HistoryAsync(null, ct);
        return history.Prescriptions;
    }

    private async Task CreateInvoiceIfMissingAsync(Appointment appointment, CancellationToken ct)
    {
        if (await billing.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id, ct))
            return;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var amount = await billing.Fees
            .Where(f => f.AppointmentTypeId == appointment.AppointmentTypeId && f.EffectiveFrom <= today)
            .OrderByDescending(f => f.EffectiveFrom)
            .Select(f => (decimal?)f.Amount)
            .FirstOrDefaultAsync(ct) ?? 0;
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
                    Description = "Consulta",
                    Amount = amount
                }
            ]
        }, ct);
    }

    private async Task EnsurePatientAccessAsync(Guid patientId, CancellationToken ct)
    {
        if (currentUser.Role != TenantRole.Patient.ToString())
            return;
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
