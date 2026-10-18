import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentDto, AppointmentsService, ClinicalService, DiagnosisDto, errorMessage } from 'sdk';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { FechaPipe, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, FechaPipe, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './doctor.html',
})
export class DoctorPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly clinical = inject(ClinicalService);
  readonly statusLabels = appointmentStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly diagnoses = signal<DiagnosisDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly saving = signal(false);
  readonly closing = signal(false);
  readonly notice = signal('');
  readonly visit = signal<AppointmentDto | null>(null);
  date = new Date().toISOString().slice(0, 10);
  appointmentId = '';
  encounterId = '';
  note = '';
  bloodPressure = '';
  heartRate: number | null = null;
  temperature: number | null = null;
  weightKg: number | null = null;
  diagnosisIds: string[] = [];
  medication = '';
  dose = '';
  frequency = '';
  duration = '';
  instructions = '';

  constructor() {
    void this.load();
  }

  async load(quiet = false) {
    if (!quiet && !this.loading()) this.refreshing.set(true);
    try {
      const [appointments, diagnoses] = await Promise.all([
        firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: this.date })),
        firstValueFrom(this.clinical.diagnoses()),
      ]);
      this.appointments.set(appointments);
      this.diagnoses.set(diagnoses);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
      this.refreshing.set(false);
    }
  }

  pick(appointment: AppointmentDto) {
    this.visit.set(appointment);
    this.appointmentId = appointment.id;
    this.encounterId = '';
    this.notice.set('');
    this.error.set('');
    this.note = '';
    this.bloodPressure = '';
    this.heartRate = null;
    this.temperature = null;
    this.weightKg = null;
    this.diagnosisIds = [];
    this.medication = '';
    this.dose = '';
    this.frequency = '';
    this.duration = '';
    this.instructions = '';
  }

  async save() {
    if (await this.persist()) this.notice.set('Consulta guardada.');
  }

  async close() {
    this.closing.set(true);
    try {
      if (!(await this.persist())) return;
      await firstValueFrom(this.clinical.closeEncounter(this.encounterId));
      this.notice.set('Consulta cerrada. El cobro quedó para secretaría.');
      await this.load(true);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.closing.set(false);
    }
  }

  private async persist() {
    if (!this.appointmentId) return false;
    const medication = this.medication.trim();
    if (medication && (!this.dose.trim() || !this.frequency.trim() || !this.duration.trim())) {
      this.error.set('La receta necesita dosis, frecuencia y duración.');
      return false;
    }
    this.error.set('');
    this.notice.set('');
    this.saving.set(true);
    try {
      const encounter = await firstValueFrom(
        this.clinical.saveEncounter({
          appointmentId: this.appointmentId,
          note: this.note,
          bloodPressure: this.bloodPressure || null,
          heartRate: this.heartRate,
          temperature: this.temperature,
          weightKg: this.weightKg,
          diagnosisIds: this.diagnosisIds,
        }),
      );
      this.encounterId = encounter.id;
      if (medication) {
        await firstValueFrom(
          this.clinical.createPrescription({
            encounterId: encounter.id,
            instructions: this.instructions,
            items: [{ medication, dose: this.dose.trim(), frequency: this.frequency.trim(), duration: this.duration.trim() }],
          }),
        );
        this.medication = '';
        this.dose = '';
        this.frequency = '';
        this.duration = '';
        this.instructions = '';
      }
      await this.load(true);
      return true;
    } catch (error) {
      this.error.set(errorMessage(error));
      return false;
    } finally {
      this.saving.set(false);
    }
  }
}
