import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentStatus, AppointmentsService, ClinicalService, DiagnosisDto, EncounterDto, errorMessage } from 'sdk';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { AppRoute, patientParams } from '../../app.routes.enum';
import { FechaPipe, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { SignModal } from './sign-modal';

// The visit is a draft while the professional writes it and can be saved and reopened. Signing it makes it the
// next folio of the patient's record and closes the appointment for billing; from then on it is only corrected.
@Component({
  imports: [FormsModule, RouterLink, FechaPipe, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable, SignModal],
  templateUrl: './doctor.html',
})
export class DoctorPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly clinical = inject(ClinicalService);
  readonly statusLabels = appointmentStatusLabels;
  readonly statuses = AppointmentStatus;
  readonly route = AppRoute;
  readonly patientParams = patientParams;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly pending = signal<EncounterDto[]>([]);
  readonly matches = signal<DiagnosisDto[]>([]);
  readonly chosen = signal<DiagnosisDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly saving = signal(false);
  readonly opening = signal(false);
  readonly signing = signal(false);
  readonly signError = signal('');
  readonly asking = signal(false);
  readonly notice = signal('');
  readonly visit = signal<{ appointmentId: string; patientId: string; patient: string; start: string; type?: string } | null>(null);
  date = new Date().toISOString().slice(0, 10);
  encounterId = '';
  note = '';
  plan = '';
  bloodPressure = '';
  heartRate: number | null = null;
  temperature: number | null = null;
  weightKg: number | null = null;
  search = '';
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    void this.load();
  }

  async load(quiet = false) {
    if (!quiet && !this.loading()) this.refreshing.set(true);
    try {
      const [appointments, pending] = await Promise.all([
        firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: this.date })),
        firstValueFrom(this.clinical.pendingEncounters()),
      ]);
      this.appointments.set(appointments);
      this.pending.set(pending);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
      this.refreshing.set(false);
    }
  }

  canAttend(appointment: AppointmentDto) {
    return ![AppointmentStatus.Cancelled, AppointmentStatus.NoShow, AppointmentStatus.Completed].includes(appointment.status);
  }

  pick(appointment: AppointmentDto) {
    void this.open({
      appointmentId: appointment.id,
      patientId: appointment.patientId,
      patient: appointment.patient,
      start: appointment.start,
      type: appointment.appointmentType,
    });
  }

  resume(encounter: EncounterDto) {
    void this.open({ appointmentId: encounter.appointmentId, patientId: encounter.patientId, patient: encounter.patient, start: encounter.appointmentStart });
  }

  // Reopening a visit brings back the draft that was saved, so nothing written is lost or overwritten blank.
  private async open(visit: { appointmentId: string; patientId: string; patient: string; start: string; type?: string }) {
    this.visit.set(visit);
    this.notice.set('');
    this.error.set('');
    this.opening.set(true);
    try {
      this.fill(await firstValueFrom(this.clinical.encounterForAppointment(visit.appointmentId)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.opening.set(false);
    }
  }

  private fill(encounter: EncounterDto | null) {
    this.encounterId = encounter?.id ?? '';
    this.note = encounter?.note ?? '';
    this.plan = encounter?.plan ?? '';
    this.bloodPressure = encounter?.bloodPressure ?? '';
    this.heartRate = encounter?.heartRate ?? null;
    this.temperature = encounter?.temperature ?? null;
    this.weightKg = encounter?.weightKg ?? null;
    this.chosen.set(encounter?.diagnoses ?? []);
    this.search = '';
    this.matches.set([]);
  }

  // CIE-10 by code or name; the catalog is too long to list whole.
  findDiagnoses() {
    clearTimeout(this.searchTimer);
    const text = this.search.trim();
    if (text.length < 2) {
      this.matches.set([]);
      return;
    }
    this.searchTimer = setTimeout(async () => {
      try {
        const found = await firstValueFrom(this.clinical.diagnoses({ q: text }));
        this.matches.set(found.filter((d) => !this.chosen().some((c) => c.id === d.id)));
      } catch (error) {
        this.error.set(errorMessage(error));
      }
    }, 250);
  }

  addDiagnosis(diagnosis: DiagnosisDto) {
    this.chosen.update((list) => [...list, diagnosis]);
    this.matches.update((list) => list.filter((d) => d.id !== diagnosis.id));
    this.search = '';
  }

  removeDiagnosis(id: string) {
    this.chosen.update((list) => list.filter((d) => d.id !== id));
  }

  async save() {
    if (await this.persist()) this.notice.set('Borrador guardado. Todavía no está en la historia: falta firmarlo.');
  }

  async askSignature() {
    this.signError.set('');
    if (!this.note.trim()) {
      this.error.set('Para firmar hace falta la evolución.');
      return;
    }
    if (await this.persist()) this.asking.set(true);
  }

  async sign(password: string) {
    this.signing.set(true);
    this.signError.set('');
    try {
      const entry = await firstValueFrom(this.clinical.signEncounter(this.encounterId, { password }));
      this.asking.set(false);
      this.visit.set(null);
      this.notice.set(`Consulta firmada como folio ${entry.folio}. El cobro quedó para secretaría.`);
      await this.load(true);
    } catch (error) {
      this.signError.set(errorMessage(error));
    } finally {
      this.signing.set(false);
    }
  }

  private async persist() {
    const visit = this.visit();
    if (!visit) return false;
    this.error.set('');
    this.notice.set('');
    this.saving.set(true);
    try {
      const encounter = await firstValueFrom(
        this.clinical.saveEncounter({
          appointmentId: visit.appointmentId,
          note: this.note,
          plan: this.plan,
          bloodPressure: this.bloodPressure || null,
          heartRate: this.heartRate,
          temperature: this.temperature,
          weightKg: this.weightKg,
          diagnosisIds: this.chosen().map((d) => d.id),
        }),
      );
      this.encounterId = encounter.id;
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
