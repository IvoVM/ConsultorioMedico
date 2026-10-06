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
  readonly prescribing = signal(false);
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
    this.appointmentId = appointment.id;
    this.encounterId = '';
  }

  async save() {
    this.error.set('');
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
      await this.load(true);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  async close() {
    this.closing.set(true);
    try {
      if (!this.encounterId) await this.save();
      if (!this.encounterId) return;
      await firstValueFrom(this.clinical.closeEncounter(this.encounterId));
      await this.load(true);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.closing.set(false);
    }
  }

  async prescribe() {
    this.prescribing.set(true);
    try {
      await firstValueFrom(
        this.clinical.createPrescription({
          encounterId: this.encounterId,
          instructions: this.instructions,
          items: [{ medication: this.medication, dose: this.dose, frequency: this.frequency, duration: this.duration }],
        }),
      );
      this.medication = '';
      this.error.set('Receta emitida.');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.prescribing.set(false);
    }
  }
}
