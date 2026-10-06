import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MedicalRecordDto, MedicalRecordsService, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { BLOOD_TYPES, MedicalRecordForm, age, shortDate, toForm } from './models/medical-record-form';
import { MedicalRecordFilter } from './models/record-filter';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiTable],
  templateUrl: './medical-records.html',
})
export class MedicalRecordsPage {
  private readonly recordsApi = inject(MedicalRecordsService);
  readonly bloodTypes = BLOOD_TYPES;
  readonly age = age;
  readonly shortDate = shortDate;
  readonly filters: { id: MedicalRecordFilter; label: string }[] = [
    { id: 'all', label: 'Todas' },
    { id: 'allergies', label: 'Con alergias' },
    { id: 'incomplete', label: 'Sin completar' },
  ];

  readonly records = signal<MedicalRecordDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
  readonly filter = signal<MedicalRecordFilter>('all');
  readonly openId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly justSaved = signal<string | null>(null);
  draft: MedicalRecordForm | null = null;

  readonly visible = computed(() => {
    const text = this.search().trim().toLowerCase();
    return this.records().filter((r) => {
      if (this.filter() === 'allergies' && !r.allergies) return false;
      if (this.filter() === 'incomplete' && r.updatedAt) return false;
      if (!text) return true;
      return `${r.firstName} ${r.lastName} ${r.documentNumber} ${r.email}`.toLowerCase().includes(text);
    });
  });

  readonly counts = computed(() => ({
    all: this.records().length,
    allergies: this.records().filter((r) => r.allergies).length,
    incomplete: this.records().filter((r) => !r.updatedAt).length,
  }));

  constructor() {
    void firstValueFrom(this.recordsApi.medicalRecords())
      .then((list) => this.records.set(list))
      .catch((error) => this.error.set(errorMessage(error)))
      .finally(() => this.loading.set(false));
  }

  toggle(record: MedicalRecordDto) {
    if (this.openId() === record.patientId) {
      this.openId.set(null);
      this.draft = null;
      return;
    }
    this.error.set('');
    this.draft = toForm(record);
    this.openId.set(record.patientId);
  }

  async save(patientId: string) {
    if (!this.draft) return;
    this.saving.set(true);
    this.error.set('');
    try {
      const saved = await firstValueFrom(this.recordsApi.saveMedicalRecord(patientId, this.draft));
      this.records.update((list) => list.map((r) => (r.patientId === patientId ? saved : r)));
      this.openId.set(null);
      this.draft = null;
      this.justSaved.set(patientId);
      setTimeout(() => this.justSaved.update((id) => (id === patientId ? null : id)), 2400);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
