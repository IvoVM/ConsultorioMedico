import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MedicalRecordDto, MedicalRecordsService, errorMessage } from 'sdk';
import { UiButton, UiField, UiSkeleton, formatPhone, UiHelp } from 'ui';
import { firstValueFrom } from 'rxjs';
import { PatientForm, age, contactAgreementLabel, shortDate, toForm } from './models/patient-form';

@Component({
  imports: [FormsModule, RouterLink, UiHelp, UiButton, UiField, UiSkeleton],
  templateUrl: './patient-file.html',
})
export class PatientFilePage {
  private readonly recordsApi = inject(MedicalRecordsService);
  private readonly patientId = inject(ActivatedRoute).snapshot.paramMap.get('patientId') ?? '';
  readonly shortDate = shortDate;
  readonly today = new Date().toISOString().slice(0, 10);
  readonly temporaryPassword = signal<string>(history.state?.temporaryPassword ?? '');
  readonly copied = signal(false);
  readonly sections = [
    { id: 'identity', label: 'Datos personales' },
    { id: 'coverage', label: 'Cobertura' },
    { id: 'emergency', label: 'Contacto de emergencia' },
  ];

  readonly record = signal<MedicalRecordDto | null>(null);
  readonly form = signal<PatientForm | null>(null);
  readonly original = signal('');
  readonly error = signal('');
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly version = signal(0);
  readonly contactLabel = contactAgreementLabel;
  readonly contactAgreed = signal(false);
  // The contact changed from what is stored (or was never confirmed), so the admin has to confirm it again.
  readonly contactNeedsAgreement = computed(() => {
    this.version();
    const form = this.form();
    const record = this.record();
    if (!form || !record) return false;
    if (!form.emergencyContact.trim() && !form.emergencyPhone.trim()) return false;
    const changed = form.emergencyContact.trim() !== (record.emergencyContact ?? '') || (formatPhone(form.emergencyPhone) ?? form.emergencyPhone.trim()) !== (record.emergencyPhone ?? '');
    return changed || !record.emergencyContactAgreedAt;
  });

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
    if (this.contactNeedsAgreement() && !this.contactAgreed()) {
      this.error.set('Confirmá con el paciente que su contacto de emergencia aceptó que lo llamemos.');
      return;
    }
    const next = { ...form, phone, emergencyPhone: emergencyPhone ?? '' };
    this.form.set(next);
    this.saving.set(true);
    this.error.set('');
    try {
      this.apply(await firstValueFrom(this.recordsApi.saveMedicalRecord(this.patientId, { ...next, emergencyContactAgreed: this.contactNeedsAgreement() && this.contactAgreed() })));
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

  private apply(record: MedicalRecordDto) {
    const form = toForm(record);
    this.record.set(record);
    this.form.set(form);
    this.original.set(JSON.stringify(form));
    this.contactAgreed.set(false);
    this.version.update((value) => value + 1);
  }
}
