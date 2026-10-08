using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Identity;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure;

public class DemoDataSeeder(TenantDbContext db, UserManager<TenantUser> users)
{
    private string password = "";
    private TimeZoneInfo zone = TimeZoneInfo.Utc;
    private Dictionary<string, Guid> diagnoses = [];

    public async Task SeedAsync(string seedPassword, TimeZoneInfo timeZone, Guid? adminId, CancellationToken ct)
    {
        if (await db.Specialties.AnyAsync(ct))
            return;

        password = seedPassword;
        zone = timeZone;
        diagnoses = await db.Diagnoses.ToDictionaryAsync(d => d.Code, d => d.Id, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
        var recorded = (await db.MedicalRecords.Select(r => r.ClientId).ToListAsync(ct)).ToHashSet();
        var clinicaId = Guid.NewGuid();
        var pediatriaId = Guid.NewGuid();
        var traumaId = Guid.NewGuid();
        var cardioId = Guid.NewGuid();

        var martin = await AccountAsync("medico@demo.local", "Martín", "Ríos", TenantRole.Doctor, "MN 12345", clinicaId, ct);
        var elena = await AccountAsync("elena.vazquez@demo.local", "Elena", "Vázquez", TenantRole.Doctor, "MN 22810", pediatriaId, ct);
        var julian = await AccountAsync("julian.costa@demo.local", "Julián", "Costa", TenantRole.Doctor, "MN 33402", traumaId, ct);
        var sofia = await AccountAsync("sofia.herrera@demo.local", "Sofía", "Herrera", TenantRole.Doctor, "MN 19077", cardioId, ct);
        var anaUser = await users.FindByEmailAsync("paciente@demo.local")
            ?? throw new InvalidOperationException("Falta la paciente de demostración.");
        var ana = await db.Clients.FirstAsync(c => c.UserId == anaUser.Id, ct);
        var bruno = await PatientAsync("bruno.diaz@demo.local", "Bruno", "Díaz", "28441990", new DateOnly(1985, 11, 3), "1155550101", ct);
        var camila = await PatientAsync("camila.ortiz@demo.local", "Camila", "Ortiz", "45667801", new DateOnly(2016, 7, 22), "1166660202", ct);
        var diego = await PatientAsync("diego.fernandez@demo.local", "Diego", "Fernández", "25990112", new DateOnly(1978, 2, 14), "1177770303", ct);
        var mateo = await PatientAsync("mateo.ruiz@demo.local", "Mateo", "Ruiz", "50123456", new DateOnly(2018, 1, 8), "1188880404", ct);
        var nora = await PatientAsync("nora.acosta@demo.local", "Nora", "Acosta", "22334455", new DateOnly(1962, 5, 19), "1199990505", ct);
        var lucia = await PatientAsync("lucia.romero@demo.local", "Lucía", "Romero", "33221098", new DateOnly(1994, 9, 30), "1140406060", ct);
        var hugo = await PatientAsync("hugo.pereyra@demo.local", "Hugo", "Pereyra", "27889900", new DateOnly(1972, 3, 2), "1130307070", ct);
        var valentina = await PatientAsync("valentina.sosa@demo.local", "Valentina", "Sosa", "36778821", new DateOnly(1988, 12, 1), "1120208080", ct);

        var main = await db.Locations.OrderBy(l => l.Name).FirstAsync(ct);
        if (string.IsNullOrWhiteSpace(main.Address))
            main.Address = "Av. Santa Fe 1840, CABA";

        var anexo = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Anexo Thames",
            Address = "Thames 1420, CABA",
            IsActive = true
        };
        db.Locations.Add(anexo);
        db.MedicalServices.AddRange(
            Service(main.Id, "Consultorios externos"),
            Service(main.Id, "Vacunatorio"),
            Service(anexo.Id, "Diagnóstico por imágenes"));

        var clinica = AddSpecialty(clinicaId, "Clínica médica");
        var pediatria = AddSpecialty(pediatriaId, "Pediatría");
        var trauma = AddSpecialty(traumaId, "Traumatología");
        var cardio = AddSpecialty(cardioId, "Cardiología");
        var consulta = AddType("Consulta clínica", 20, clinica.Id);
        var control = AddType("Control", 15, clinica.Id);
        var nino = AddType("Consulta pediátrica", 30, pediatria.Id);
        var osteo = AddType("Consulta traumatológica", 30, trauma.Id);
        var ecg = AddType("Electrocardiograma", 20, cardio.Id);
        db.Fees.AddRange(
            NewFee(consulta.Id, 22000m, new DateOnly(2026, 1, 1)),
            NewFee(consulta.Id, 25000m, new DateOnly(today.Year, today.Month, 1)),
            NewFee(control.Id, 15000m, new DateOnly(2026, 1, 1)),
            NewFee(nino.Id, 24000m, new DateOnly(2026, 1, 1)),
            NewFee(osteo.Id, 28000m, new DateOnly(2026, 1, 1)),
            NewFee(ecg.Id, 18000m, new DateOnly(2026, 1, 1)));

        Weekdays(martin.Id, new TimeOnly(8, 0), new TimeOnly(12, 0), main.Id, consulta.Id);
        Weekdays(elena.Id, new TimeOnly(9, 0), new TimeOnly(13, 0), main.Id, nino.Id);
        Weekdays(julian.Id, new TimeOnly(10, 0), new TimeOnly(14, 0), anexo.Id, osteo.Id);
        Weekdays(sofia.Id, new TimeOnly(15, 0), new TimeOnly(18, 0), main.Id, ecg.Id);

        Blockout(martin.Id, today.AddDays(6), 8, 0, 12, 0, "Congreso de clínica");
        Blockout(elena.Id, today.AddDays(10), 9, 0, 12, 0, "Jornada de pediatría");
        Blockout(julian.Id, today.AddDays(9), 10, 0, 13, 0, "Capacitación");
        Blockout(sofia.Id, today.AddDays(8), 15, 0, 18, 0, "Congreso de cardiología");

        File(recorded, ana, adminId, "O+", "Penicilina", "Asma en la infancia.", "Madre con hipertensión.", "Salbutamol a demanda.", "Camina tres veces por semana.", "OSDE", "210045778", "Carlos Pérez", "1144448899", "Prefiere los turnos de la mañana.");
        File(recorded, bruno, adminId, "A-", "Polen", "Rinitis estacional.", "Padre con diabetes.", null, "No fuma.", "Swiss Medical", "SM-8821", "Laura Díaz", "1155550199", null);
        File(recorded, camila, adminId, "O+", null, "Controles pediátricos al día.", null, null, null, "IOMA", "IO-4412", "Marina Ortiz", "1166660299", "Viene con la madre.");
        File(recorded, diego, adminId, "B+", null, "Lumbago recurrente.", null, "Ibuprofeno ocasional.", "Trabajo de pie.", null, null, "Silvia Fernández", "1177770399", "Particular, sin cobertura.");
        File(recorded, mateo, adminId, "A+", null, "Desarrollo acorde a la edad.", null, null, null, "OSDE", "210088331", "Pablo Ruiz", "1188880499", null);
        File(recorded, nora, adminId, "AB+", "AAS", "Hipertensión en tratamiento.", "Hermanos con cardiopatía.", "Enalapril 10 mg.", "Camina todas las mañanas.", "PAMI", "PA-22910", "Rosa Acosta", "1199990599", null);
        File(recorded, lucia, adminId, "O-", null, "Palpitaciones ocasionales.", null, null, "Toma café por la mañana.", "Galeno", "GL-1002", "Andrés Romero", "1140406099", null);
        File(recorded, hugo, adminId, "A+", null, "Resfríos de repetición en invierno.", null, null, null, "OSDE", "210077120", "Marta Pereyra", "1130307099", null);
        File(recorded, valentina, adminId, "B-", null, "Dolor de rodilla al subir escaleras.", null, null, "Corre dos veces por semana.", "Medicus", "ME-5520", "Iván Sosa", "1120208099", "Pide turno de traumatología.");

        var lumbago = new Rx("Tomar con comida. Si el dolor sigue, volver.", [("Ibuprofeno 400 mg", "1 comprimido", "cada 8 horas", "3 días")]);
        var resfrio = new Rx("Reposo e hidratación. Control si aparece fiebre alta.", [("Paracetamol 500 mg", "1 comprimido", "cada 8 horas", "3 días")]);
        var nina = new Rx("Completar el tratamiento aunque mejore.", [("Amoxicilina 250 mg/5 ml", "5 ml", "cada 8 horas", "7 días")]);
        var presion = new Rx("No suspender sin consulta.", [("Enalapril 10 mg", "1 comprimido", "por la mañana", "30 días")]);

        AddVisit(ana, martin, main.Id, consulta.Id, today.AddDays(-18), 9, 0, 20, AppointmentStatus.Completed, "Dolor de espalda",
            new Chart("Lumbago de cuatro días, sin irradiación. Movilidad conservada.", "118/76", 74, 36.4m, 64.2m, true, ["M54.5"], lumbago),
            new Bill(22000m, "Consulta clínica", InvoiceStatus.Paid, PaymentMethod.Cash));
        AddVisit(ana, martin, main.Id, consulta.Id, today.AddDays(-1), 11, 0, 20, AppointmentStatus.Cancelled, "Control", null, null);
        AddVisit(ana, martin, main.Id, consulta.Id, today, 8, 40, 20, AppointmentStatus.Booked, "Control de presión", null, null);
        AddVisit(ana, martin, main.Id, control.Id, today.AddDays(7), 11, 0, 15, AppointmentStatus.Booked, "Control", null, null);

        AddVisit(bruno, martin, main.Id, consulta.Id, today.AddDays(-8), 10, 0, 20, AppointmentStatus.Completed, "Tos y congestión",
            new Chart("Cuadro viral de tres días. Faringe congestiva, sin foco pulmonar.", "128/82", 80, 37.2m, 81.5m, true, ["J06.9"], resfrio),
            new Bill(22000m, "Consulta clínica", InvoiceStatus.Pending, null));
        AddVisit(bruno, martin, main.Id, consulta.Id, today, 9, 0, 20, AppointmentStatus.CheckedIn, "Dolor de garganta", null, null);

        AddVisit(camila, elena, main.Id, nino.Id, today.AddDays(-15), 9, 30, 30, AppointmentStatus.Completed, "Dolor de oído",
            new Chart("Otitis media aguda. Tímpano derecho abombado.", null, 96, 37.6m, 22.4m, true, ["J06.9"], nina),
            new Bill(24000m, "Consulta pediátrica", InvoiceStatus.Paid, PaymentMethod.Card));
        AddVisit(camila, elena, main.Id, nino.Id, today, 9, 30, 30, AppointmentStatus.Booked, "Control de niño sano", null, null);

        AddVisit(diego, julian, anexo.Id, osteo.Id, today.AddDays(-11), 10, 30, 30, AppointmentStatus.Completed, "Dolor lumbar",
            new Chart("Contractura paravertebral. Sin déficit motor.", "130/84", 72, 36.5m, 88m, true, ["M54.5"], lumbago),
            new Bill(28000m, "Consulta traumatológica", InvoiceStatus.Paid, PaymentMethod.Transfer));
        AddVisit(diego, julian, anexo.Id, osteo.Id, today, 10, 30, 30, AppointmentStatus.CheckedIn, "Control de rodilla", null, null);

        AddVisit(mateo, elena, main.Id, nino.Id, today.AddDays(-6), 11, 0, 30, AppointmentStatus.Completed, "Control de crecimiento",
            new Chart("Peso y talla en carril. Alimentación variada.", null, 92, 36.6m, 14.8m, true, ["J00"], null),
            new Bill(24000m, "Consulta pediátrica", InvoiceStatus.Paid, PaymentMethod.Cash));
        AddVisit(mateo, elena, main.Id, nino.Id, today, 10, 0, 30, AppointmentStatus.InProgress, "Tos nocturna", null, null);
        AddVisit(mateo, elena, main.Id, nino.Id, today.AddDays(4), 11, 30, 30, AppointmentStatus.Booked, "Control", null, null);

        AddVisit(nora, sofia, main.Id, ecg.Id, today.AddDays(-9), 15, 20, 20, AppointmentStatus.Completed, "Control de presión",
            new Chart("Hipertensión conocida, estable. Electrocardiograma sin cambios agudos.", "146/88", 68, 36.3m, 71m, true, ["I10"], presion),
            new Bill(18000m, "Electrocardiograma", InvoiceStatus.Pending, null));
        AddVisit(nora, sofia, main.Id, ecg.Id, today, 15, 0, 20, AppointmentStatus.Booked, "Control", null, null);

        AddVisit(lucia, sofia, main.Id, ecg.Id, today.AddDays(-4), 16, 0, 20, AppointmentStatus.Completed, "Palpitaciones",
            new Chart("Palpitaciones breves, sin dolor. Ritmo sinusal en el trazado.", "110/70", 78, 36.5m, 58.4m, true, ["I10"], null),
            new Bill(18000m, "Electrocardiograma", InvoiceStatus.Voided, null));
        AddVisit(lucia, sofia, main.Id, ecg.Id, today, 15, 40, 20, AppointmentStatus.Booked, "Control del trazado", null, null);
        AddVisit(lucia, sofia, main.Id, ecg.Id, today.AddDays(5), 16, 20, 20, AppointmentStatus.Booked, "Control", null, null);

        AddVisit(hugo, martin, main.Id, consulta.Id, today, 8, 0, 20, AppointmentStatus.Completed, "Resfrío",
            new Chart("Rinorrea y odinofagia de dos días. Buen estado general.", "122/78", 76, 37.1m, 79m, true, ["J00"], resfrio),
            new Bill(25000m, "Consulta clínica", InvoiceStatus.Pending, null));

        AddVisit(valentina, martin, main.Id, consulta.Id, today.AddDays(-20), 9, 40, 20, AppointmentStatus.Completed, "Dolor de rodilla",
            new Chart("Dolor mecánico de rodilla derecha. Se indica evaluación traumatológica.", "116/74", 70, 36.4m, 62m, true, ["M54.5"], null),
            new Bill(22000m, "Consulta clínica", InvoiceStatus.Paid, PaymentMethod.Transfer));

        db.WaitlistEntries.AddRange(
            new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                ClientId = valentina.Id,
                LocationId = anexo.Id,
                SpecialtyId = trauma.Id,
                Status = WaitlistStatus.Pending,
                CreatedAt = At(today.AddDays(-2), 11, 15),
                Notes = "Dolor de rodilla. Prefiere el anexo, por la mañana."
            },
            new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                ClientId = camila.Id,
                ProfessionalId = elena.Id,
                LocationId = main.Id,
                SpecialtyId = pediatria.Id,
                Status = WaitlistStatus.Offered,
                CreatedAt = At(today.AddDays(-1), 16, 40),
                Notes = "Si se libera un hueco más temprano, la madre puede acercarse."
            });

        var stamp = DateTimeOffset.UtcNow;
        db.AuditEntries.AddRange(
            Audit(adminId, stamp.AddDays(-12), "alta", "Paciente", bruno.Id, "Bruno Díaz"),
            Audit(adminId, stamp.AddDays(-11), "alta", "Paciente", camila.Id, "Camila Ortiz"),
            Audit(martin.Id, stamp.AddDays(-8), "encuentro", "Encuentro", bruno.Id, "Consulta clínica"),
            Audit(adminId, stamp.AddDays(-6), "agenda", "AgendaSemanal", martin.Id, "5 bloques"),
            Audit(elena.Id, stamp.AddDays(-6), "receta", "Receta", camila.Id, "Amoxicilina"),
            Audit(adminId, stamp.AddDays(-3), "arancel", "Arancel", consulta.Id, "25000.00"),
            Audit(sofia.Id, stamp.AddDays(-1), "reserva", "Turno", nora.Id, "Control"),
            Audit(adminId, stamp.AddHours(-3), "cobro", "Comprobante", hugo.Id, "Pendiente de cobro"));

        await db.SaveChangesAsync(ct);
    }

    private async Task<TenantUser> AccountAsync(
        string email,
        string firstName,
        string lastName,
        TenantRole role,
        string? license,
        Guid? specialtyId,
        CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new TenantUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                Role = role
            };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(' ', result.Errors.Select(e => e.Description)));
        }

        if (role != TenantRole.Patient)
            await EnsureEmployeeAsync(user.Id, license, specialtyId, ct);
        return user;
    }

    private async Task EnsureEmployeeAsync(Guid userId, string? license, Guid? specialtyId, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.UserId == userId, ct);
        if (employee is null)
        {
            db.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LicenseNumber = license,
                SpecialtyId = specialtyId
            });
            return;
        }

        if (license is not null)
            employee.LicenseNumber = license;
        if (specialtyId is Guid specialty)
            employee.SpecialtyId = specialty;
    }

    private async Task<Client> PatientAsync(
        string email,
        string firstName,
        string lastName,
        string document,
        DateOnly birth,
        string phone,
        CancellationToken ct)
    {
        var user = await AccountAsync(email, firstName, lastName, TenantRole.Patient, null, null, ct);
        var existing = await db.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id, ct);
        if (existing is not null)
            return existing;

        var patient = new Client
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentNumber = document,
            BirthDate = birth,
            Phone = phone
        };
        db.Clients.Add(patient);
        return patient;
    }

    private void File(
        HashSet<Guid> recorded,
        Client patient,
        Guid? updatedBy,
        string? blood,
        string? allergies,
        string? personal,
        string? family,
        string? medication,
        string? habits,
        string? insurance,
        string? member,
        string? emergency,
        string? emergencyPhone,
        string? notes)
    {
        if (!recorded.Add(patient.Id))
            return;

        db.MedicalRecords.Add(new MedicalRecord
        {
            Id = Guid.NewGuid(),
            ClientId = patient.Id,
            BloodType = blood,
            Allergies = allergies,
            PersonalHistory = personal,
            FamilyHistory = family,
            CurrentMedication = medication,
            Habits = habits,
            HealthInsurance = insurance,
            MemberNumber = member,
            EmergencyContact = emergency,
            EmergencyPhone = emergencyPhone,
            Notes = notes,
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedBy = updatedBy
        });
    }

    private void AddVisit(
        Client patient,
        TenantUser professional,
        Guid locationId,
        Guid typeId,
        DateOnly day,
        int hour,
        int minute,
        int duration,
        AppointmentStatus status,
        string reason,
        Chart? chart,
        Bill? bill)
    {
        var start = At(day, hour, minute);
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            ClientId = patient.Id,
            ProfessionalId = professional.Id,
            LocationId = locationId,
            AppointmentTypeId = typeId,
            Start = start,
            End = start.AddMinutes(duration),
            Status = status,
            VisitReason = reason
        };
        db.Appointments.Add(appointment);
        if (chart is null)
            return;

        var encounter = new Encounter
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            ClientId = patient.Id,
            ProfessionalId = professional.Id,
            Note = chart.Note,
            BloodPressure = chart.BloodPressure,
            HeartRate = chart.HeartRate,
            Temperature = chart.Temperature,
            WeightKg = chart.WeightKg,
            IsClosed = chart.Closed,
            CreatedAt = start
        };
        db.Encounters.Add(encounter);
        foreach (var code in chart.Codes)
        {
            if (diagnoses.TryGetValue(code, out var diagnosisId))
                db.EncounterDiagnoses.Add(new EncounterDiagnosis { EncounterId = encounter.Id, DiagnosisId = diagnosisId });
        }

        if (chart.Prescription is { } rx)
        {
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                EncounterId = encounter.Id,
                ClientId = patient.Id,
                ProfessionalId = professional.Id,
                Instructions = rx.Instructions,
                CreatedAt = start.AddMinutes(15)
            };
            db.Prescriptions.Add(prescription);
            foreach (var item in rx.Items)
            {
                db.PrescriptionItems.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    Medication = item.Medication,
                    Dose = item.Dose,
                    Frequency = item.Frequency,
                    Duration = item.Duration
                });
            }
        }

        if (bill is null)
            return;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            ClientId = patient.Id,
            Total = bill.Amount,
            Status = bill.Status,
            PaymentMethod = bill.Method,
            CreatedAt = start.AddMinutes(duration),
            PaidAt = bill.Status == InvoiceStatus.Paid ? start.AddHours(2) : null
        };
        db.Invoices.Add(invoice);
        db.InvoiceItems.Add(new InvoiceItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Description = bill.Description,
            Amount = bill.Amount
        });
    }

    private void Weekdays(Guid professionalId, TimeOnly start, TimeOnly end, Guid locationId, Guid typeId)
    {
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            db.ScheduleBlocks.Add(new ScheduleBlock
            {
                Id = Guid.NewGuid(),
                ProfessionalId = professionalId,
                Day = day,
                StartTime = start,
                EndTime = end,
                LocationId = locationId,
                AppointmentTypeId = typeId
            });
        }
    }

    private void Blockout(Guid professionalId, DateOnly day, int fromHour, int fromMinute, int toHour, int toMinute, string reason)
    {
        db.ScheduleBlockouts.Add(new ScheduleBlockout
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            Start = At(day, fromHour, fromMinute),
            End = At(day, toHour, toMinute),
            Reason = reason
        });
    }

    private DateTimeOffset At(DateOnly day, int hour, int minute) =>
        SchedulingRules.Combine(day, new TimeOnly(hour, minute), zone);

    private static MedicalService Service(Guid locationId, string name) =>
        new() { Id = Guid.NewGuid(), LocationId = locationId, Name = name };

    private Specialty AddSpecialty(Guid id, string name)
    {
        var specialty = new Specialty { Id = id, Name = name };
        db.Specialties.Add(specialty);
        return specialty;
    }

    private AppointmentType AddType(string name, int minutes, Guid specialtyId)
    {
        var type = new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = name,
            DurationMinutes = minutes,
            SpecialtyId = specialtyId
        };
        db.AppointmentTypes.Add(type);
        return type;
    }

    private static Fee NewFee(Guid typeId, decimal amount, DateOnly from) =>
        new() { Id = Guid.NewGuid(), AppointmentTypeId = typeId, Amount = amount, EffectiveFrom = from };

    private static AuditEntry Audit(Guid? userId, DateTimeOffset timestamp, string action, string entity, Guid entityId, string detail) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId.ToString(),
            Detail = detail,
            Timestamp = timestamp
        };

    private sealed record Chart(
        string Note,
        string? BloodPressure,
        int? HeartRate,
        decimal? Temperature,
        decimal? WeightKg,
        bool Closed,
        string[] Codes,
        Rx? Prescription);

    private sealed record Rx(string? Instructions, (string Medication, string Dose, string Frequency, string Duration)[] Items);

    private sealed record Bill(decimal Amount, string Description, InvoiceStatus Status, PaymentMethod? Method);
}
