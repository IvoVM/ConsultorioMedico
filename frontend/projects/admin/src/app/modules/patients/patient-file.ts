import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ClinicalHistoryDto, ClinicalService, DiagnosisDto, MedicalRecordDto, MedicalRecordsService, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';
import { BLOOD_TYPES, MedicalRecordForm, age, shortDate, toForm } from '../medical-records/models/medical-record-form';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiSkeleton],
  templateUrl: './patient-file.html',
})
export class PatientFilePage {
  private readonly recordsApi = inject(MedicalRecordsService);
  private readonly clinical = inject(ClinicalService);
  private readonly patientId = inject(ActivatedRoute).snapshot.paramMap.get('patientId') ?? '';
  readonly bloodTypes = BLOOD_TYPES;
  readonly shortDate = shortDate;
  readonly today = new Date().toISOString().slice(0, 10);
  readonly temporaryPassword = signal<string>(history.state?.temporaryPassword ?? '');
  readonly copied = signal(false);
  readonly sections = [
    { id: 'identity', label: 'Datos personales' },
    { id: 'coverage', label: 'Cobertura' },
    { id: 'emergency', label: 'Contacto de emergencia' },
    { id: 'clinical', label: 'Datos clínicos' },
    { id: 'history', label: 'Antecedentes' },
    { id: 'attention', label: 'Atención clínica' },
  ];

  readonly record = signal<MedicalRecordDto | null>(null);
  readonly form = signal<MedicalRecordForm | null>(null);
  readonly original = signal('');
  readonly error = signal('');
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly version = signal(0);
  readonly historyData = signal<ClinicalHistoryDto | null>(null);
  readonly historyError = signal('');

  readonly dirty = computed(() => {
    this.version();
    const form = this.form();
    return !!form && JSON.stringify(form) !== this.original();
  });
  readonly age = computed(() => {
    this.version();
    const birthDate = this.form()?.birthDate;
    return birthDate ? age(birthDate) : null;
  });

  constructor() {
    void this.loadRecord();
    void this.loadHistory();
  }

  touch() {
    this.version.update((value) => value + 1);
    this.saved.set(false);
  }

  goTo(id: string) {
    const section = document.getElementById(id);
    if (!section) return;
    const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
    section.scrollIntoView({ behavior: reduced ? 'auto' : 'smooth', block: 'start' });
    section.querySelector<HTMLElement>('input:not([disabled]), textarea')?.focus({ preventScroll: true });
  }

  discard() {
    this.form.set(JSON.parse(this.original()));
    this.touch();
  }

  async copyPassword() {
    const value = this.temporaryPassword();
    if (!value) return;
    try {
      await navigator.clipboard.writeText(value);
      this.copied.set(true);
    } catch {
      this.copied.set(false);
    }
  }

  diagnosesText(diagnoses: DiagnosisDto[]) {
    return diagnoses.map((item) => `${item.code} ${item.name}`).join(', ');
  }

  when(value: string) {
    return new Intl.DateTimeFormat('es-AR', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  async save() {
    const form = this.form();
    if (!form) return;
    this.saving.set(true);
    this.error.set('');
    try {
      this.apply(await firstValueFrom(this.recordsApi.saveMedicalRecord(this.patientId, form)));
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), 2600);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  private async loadRecord() {
    try {
      this.apply(await firstValueFrom(this.recordsApi.medicalRecord(this.patientId)));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  private async loadHistory() {
    try {
      this.historyData.set(await firstValueFrom(this.clinical.clinicalHistory({ patientId: this.patientId })));
    } catch (error) {
      this.historyError.set(errorMessage(error));
    }
  }

  private apply(record: MedicalRecordDto) {
    const form = toForm(record);
    this.record.set(record);
    this.form.set(form);
    this.original.set(JSON.stringify(form));
    this.version.update((value) => value + 1);
  }
}
