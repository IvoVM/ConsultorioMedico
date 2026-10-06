import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentDto, ClinicaClient, DiagnosisDto, appointmentStatusLabels, errorMessage } from 'sdk';
import { UiButton, UiCalendar, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiCalendar, UiField, UiTable],
  templateUrl: './doctor.html',
})
export class DoctorPage {
  private readonly api = inject(ClinicaClient);
  readonly statusLabels = appointmentStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly diagnoses = signal<DiagnosisDto[]>([]);
  readonly error = signal('');
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

  async load() {
    try {
      const [appointments, diagnoses] = await Promise.all([
        firstValueFrom(this.api.appointmentsForDay({ date: this.date })),
        firstValueFrom(this.api.diagnoses()),
      ]);
      this.appointments.set(appointments);
      this.diagnoses.set(diagnoses);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  pick(appointment: AppointmentDto) {
    this.appointmentId = appointment.id;
    this.encounterId = '';
  }

  async save() {
    this.error.set('');
    try {
      const encounter = await firstValueFrom(
        this.api.saveEncounter({
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
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async close() {
    if (!this.encounterId) await this.save();
    if (!this.encounterId) return;
    try {
      await firstValueFrom(this.api.closeEncounter(this.encounterId));
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async prescribe() {
    try {
      await firstValueFrom(
        this.api.createPrescription({
          encounterId: this.encounterId,
          instructions: this.instructions,
          items: [{ medication: this.medication, dose: this.dose, frequency: this.frequency, duration: this.duration }],
        }),
      );
      this.medication = '';
      this.error.set('Receta emitida.');
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
