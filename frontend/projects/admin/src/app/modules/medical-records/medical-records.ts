import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MedicalRecordDto, MedicalRecordsService, errorMessage } from 'sdk';
import { MediaWatcherService, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';
import { BLOOD_TYPES, MedicalRecordForm, age, shortDate, toForm } from './models/medical-record-form';
import { MedicalRecordFilter } from './models/record-filter';

@Component({
  imports: [FormsModule, RouterLink, ScreenNote, SortCol, TablePager, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './medical-records.html',
})
export class MedicalRecordsPage {
  private readonly recordsApi = inject(MedicalRecordsService);
  private readonly media = inject(MediaWatcherService);
  private readonly router = inject(Router);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly bloodTypes = BLOOD_TYPES;
  readonly age = age;
  readonly shortDate = shortDate;
  readonly filters: { id: MedicalRecordFilter; label: string }[] = [
    { id: 'all', label: 'Todas' },
    { id: 'allergies', label: 'Con alergias' },
    { id: 'incomplete', label: 'Sin completar' },
  ];

  readonly records = signal<MedicalRecordDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly counts = signal({ all: 0, allergies: 0, incomplete: 0 });
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
  readonly filter = signal<MedicalRecordFilter>('all');
  readonly sort = signal<SortState>({ key: 'patient', dir: 'asc' });
  readonly openId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly justSaved = signal<string | null>(null);
  draft: MedicalRecordForm | null = null;

  constructor() {
    void this.load();
  }

  openFile(patientId: string, event: Event) {
    if (!this.media.mobile()) return;
    const target = event.target;
    if (target instanceof Element && target.closest('a, button, ui-button')) return;
    void this.router.navigate(['/historias', patientId]);
  }

  sortBy(key: string) {
    this.sort.update((current) => nextSort(current, key));
    this.page.set(1);
    void this.load();
  }

  onSearch(value: string) {
    this.search.set(value);
    this.page.set(1);
    queueSearch(this.searchHost, () => void this.load());
  }

  setFilter(id: MedicalRecordFilter) {
    this.filter.set(id);
    this.page.set(1);
    void this.load();
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  private async load() {
    try {
      const result = await firstValueFrom(
        this.recordsApi.medicalRecordsPage(listParams(this.page(), this.sort(), this.search(), this.filter())),
      );
      this.records.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
      this.counts.set({ all: result.all, allergies: result.allergies, incomplete: result.incomplete });
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
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
