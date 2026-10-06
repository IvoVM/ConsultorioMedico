using ClinicaSaaS.Domain;
using FluentValidation;

namespace ClinicaSaaS.Application;

public class ClinicalService(
    IClinicalStore clinical,
    IAppointmentStore appointments,
    IPatientStore patients,
    ITenantUserStore users,
    IBillingStore billing,
    ICurrentUser currentUser,
    IAuditStore audit,
    IValidator<SaveEncounterCommand> encounterValidator,
    IValidator<CreatePrescriptionCommand> prescriptionValidator,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<DiagnosisDto>> DiagnosesAsync(CancellationToken ct) =>
        (await clinical.DiagnosesAsync(ct)).Select(d => new DiagnosisDto(d.Id, d.Code, d.Name)).ToList();

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
                PatientId = appointment.PatientId,
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
        return await MapEncounterAsync(encounter, ct);
    }

    public async Task<EncounterDto> CloseEncounterAsync(Guid id, CancellationToken ct)
    {
        var encounter = await clinical.GetEncounterAsync(id, ct) ?? throw new NotFoundException("Encuentro no encontrado.");
        if (!EncounterRules.CanClose(encounter.Note))
            throw new BusinessRuleException("Para cerrar el encuentro hace falta una nota clínica.");
        if (encounter.IsClosed)
            return await MapEncounterAsync(encounter, ct);

        var appointment = await appointments.GetAsync(encounter.AppointmentId, ct) ?? throw new NotFoundException("Turno no encontrado.");
        encounter.IsClosed = true;
        appointment.Status = AppointmentStatus.Completed;
        await clinical.SaveAsync(ct);
        await appointments.SaveAsync(ct);
        await CreateInvoiceIfMissingAsync(appointment, ct);
        await audit.RecordAsync(currentUser.Id, "cierre", "Encuentro", encounter.Id.ToString(), null, ct);
        return await MapEncounterAsync(encounter, ct);
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
            PatientId = encounter.PatientId,
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
        return await MapPrescriptionAsync(prescription, ct);
    }

    public async Task<PrescriptionDto> GetPrescriptionAsync(Guid id, CancellationToken ct)
    {
        var prescription = await clinical.GetPrescriptionAsync(id, ct) ?? throw new NotFoundException("Receta no encontrada.");
        await EnsurePatientAccessAsync(prescription.PatientId, ct);
        return await MapPrescriptionAsync(prescription, ct);
    }

    public async Task<ClinicalHistoryDto> HistoryAsync(Guid? patientId, CancellationToken ct)
    {
        Patient patient;
        if (currentUser.Role == TenantRole.Patient.ToString())
        {
            patient = await patients.GetByUserAsync(currentUser.Id ?? Guid.Empty, ct)
                ?? throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
        }
        else
        {
            if (patientId is null)
                throw new BusinessRuleException("Indicá el paciente.");
            patient = await patients.GetAsync(patientId.Value, ct) ?? throw new NotFoundException("Paciente no encontrado.");
        }

        var account = await users.FindByIdAsync(patient.UserId, ct);
        var encounters = await clinical.EncountersForPatientAsync(patient.Id, ct);
        var prescriptions = await clinical.PrescriptionsForPatientAsync(patient.Id, ct);
        var encounterDtos = new List<EncounterDto>();
        foreach (var encounter in encounters.OrderByDescending(e => e.CreatedAt))
            encounterDtos.Add(await MapEncounterAsync(encounter, ct));
        var prescriptionDtos = new List<PrescriptionDto>();
        foreach (var prescription in prescriptions.OrderByDescending(p => p.CreatedAt))
            prescriptionDtos.Add(await MapPrescriptionAsync(prescription, ct));

        return new ClinicalHistoryDto(
            new PatientSummaryDto(
                patient.Id,
                account is null ? "Paciente" : $"{account.FirstName} {account.LastName}".Trim(),
                patient.DocumentNumber,
                patient.BirthDate,
                patient.Phone),
            encounterDtos,
            prescriptionDtos);
    }

    public async Task<IReadOnlyList<PrescriptionDto>> MyPrescriptionsAsync(CancellationToken ct)
    {
        var history = await HistoryAsync(null, ct);
        return history.Prescriptions;
    }

    private async Task CreateInvoiceIfMissingAsync(Appointment appointment, CancellationToken ct)
    {
        if (await billing.GetInvoiceByAppointmentAsync(appointment.Id, ct) is not null)
            return;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var fee = await billing.CurrentFeeAsync(appointment.AppointmentTypeId, today, ct);
        var amount = fee?.Amount ?? 0;
        await billing.AddInvoiceAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
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
        var patient = await patients.GetByUserAsync(currentUser.Id ?? Guid.Empty, ct)
            ?? throw new NotFoundException("No hay un paciente asociado a esta cuenta.");
        if (patient.Id != patientId)
            throw new BusinessRuleException("No podés ver datos de otra persona.");
    }

    private async Task<EncounterDto> MapEncounterAsync(Encounter encounter, CancellationToken ct)
    {
        var diagnoses = await clinical.DiagnosesForEncounterAsync(encounter.Id, ct);
        return new EncounterDto(
            encounter.Id,
            encounter.AppointmentId,
            encounter.PatientId,
            encounter.ProfessionalId,
            encounter.Note,
            encounter.BloodPressure,
            encounter.HeartRate,
            encounter.Temperature,
            encounter.WeightKg,
            encounter.IsClosed,
            encounter.CreatedAt,
            diagnoses.Select(d => new DiagnosisDto(d.Id, d.Code, d.Name)).ToList());
    }

    private async Task<PrescriptionDto> MapPrescriptionAsync(Prescription prescription, CancellationToken ct)
    {
        var professional = await users.FindByIdAsync(prescription.ProfessionalId, ct);
        var items = prescription.Items.Count > 0 ? prescription.Items : (await clinical.PrescriptionItemsAsync(prescription.Id, ct)).ToList();
        return new PrescriptionDto(
            prescription.Id,
            prescription.EncounterId,
            prescription.PatientId,
            prescription.ProfessionalId,
            professional is null ? "Profesional" : $"{professional.FirstName} {professional.LastName}".Trim(),
            prescription.Instructions,
            prescription.CreatedAt,
            items.Select(i => new PrescriptionItemDto(i.Medication, i.Dose, i.Frequency, i.Duration)).ToList());
    }
}
