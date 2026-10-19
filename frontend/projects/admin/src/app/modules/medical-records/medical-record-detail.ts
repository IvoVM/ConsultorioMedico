import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MedicalRecordDto, MedicalRecordsService, errorMessage } from 'sdk';
import { UiButton, UiField, UiSkeleton, formatPhone } from 'ui';
import { ScreenNote } from '../../screen-note';
import { firstValueFrom } from 'rxjs';
import { BLOOD_TYPES, MedicalRecordForm, age, shortDate, toForm } from './models/medical-record-form';

@Component({
  imports: [FormsModule, RouterLink, ScreenNote, UiButton, UiField, UiSkeleton],
  templateUrl: './medical-record-detail.html',
})
export class MedicalRecordDetailPage {
  private readonly recordsApi = inject(MedicalRecordsService);
  private readonly patientId = inject(ActivatedRoute).snapshot.paramMap.get('patientId') ?? '';
  readonly bloodTypes = BLOOD_TYPES;
  readonly shortDate = shortDate;
  readonly sections = [
    { id: 'identity', label: 'Datos personales' },
    { id: 'coverage', label: 'Cobertura' },
    { id: 'clinical', label: 'Datos clínicos' },
    { id: 'history', label: 'Antecedentes' },
    { id: 'emergency', label: 'Contacto de emergencia' },
  ];

  readonly record = signal<MedicalRecordDto | null>(null);
  readonly form = signal<MedicalRecordForm | null>(null);
  readonly original = signal('');
  readonly error = signal('');
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly version = signal(0);

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
    void firstValueFrom(this.recordsApi.medicalRecord(this.patientId))
      .then((record) => this.load(record))
      .catch((error) => this.error.set(errorMessage(error)));
  }

  touch() {
    this.version.update((v) => v + 1);
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
  }

  normalizePhone(key: 'phone' | 'emergencyPhone') {
    const form = this.form();
    if (!form) return;
    const formatted = formatPhone(form[key]);
    if (formatted) this.form.set({ ...form, [key]: formatted });
  }

  async save() {
    const form = this.form();
    if (!form) return;
    const phone = formatPhone(form.phone);
    const emergencyPhone = form.emergencyPhone.trim() ? formatPhone(form.emergencyPhone) : '';
    if (!phone || (form.emergencyPhone.trim() && !emergencyPhone)) {
      this.error.set('El teléfono no es válido.');
      return;
    }
    const next = { ...form, phone, emergencyPhone: emergencyPhone ?? '' };
    this.form.set(next);
    this.saving.set(true);
    this.error.set('');
    try {
      this.load(await firstValueFrom(this.recordsApi.saveMedicalRecord(this.patientId, next)));
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), 2600);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  private load(record: MedicalRecordDto) {
    const form = toForm(record);
    this.record.set(record);
    this.form.set(form);
    this.original.set(JSON.stringify(form));
  }
}
