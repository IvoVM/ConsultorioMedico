import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MedicalRecordDto, MedicalRecordsService, PatientsService, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';
import { age } from '../medical-records/models/medical-record-form';

@Component({
  imports: [FormsModule, RouterLink, ScreenNote, SortCol, TablePager, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './patients.html',
})
export class PatientsPage {
  private readonly recordsApi = inject(MedicalRecordsService);
  private readonly patientsApi = inject(PatientsService);
  private readonly router = inject(Router);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly age = age;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly patients = signal<MedicalRecordDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  readonly altaOpen = signal(false);
  readonly saving = signal(false);

  firstName = '';
  lastName = '';
  email = '';
  documentNumber = '';
  birthDate = '';
  phone = '';
  healthInsurance = '';
  memberNumber = '';
  emergencyContact = '';
  emergencyPhone = '';

  constructor() {
    void this.load();
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

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  private async load() {
    try {
      const result = await firstValueFrom(
        this.recordsApi.medicalRecordsPage(listParams(this.page(), this.sort(), this.search())),
      );
      this.patients.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  openAlta() {
    this.error.set('');
    this.altaOpen.set(true);
    queueMicrotask(() => document.getElementById('alta-nombre')?.focus());
  }

  async create() {
    this.saving.set(true);
    this.error.set('');
    try {
      const created = await firstValueFrom(
        this.patientsApi.createPatient({
          email: this.email.trim(),
          firstName: this.firstName.trim(),
          lastName: this.lastName.trim(),
          documentNumber: this.documentNumber.trim(),
          birthDate: this.birthDate,
          phone: this.phone.trim(),
          healthInsurance: this.healthInsurance.trim() || null,
          memberNumber: this.memberNumber.trim() || null,
          emergencyContact: this.emergencyContact.trim() || null,
          emergencyPhone: this.emergencyPhone.trim() || null,
        }),
      );
      await this.router.navigate(['/pacientes', created.patient.patientId], {
        state: { temporaryPassword: created.temporaryPassword },
      });
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
