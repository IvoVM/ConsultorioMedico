import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MedicalRecordDto, MedicalRecordsService, PatientsService, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { age } from '../medical-records/models/medical-record-form';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiTable],
  templateUrl: './patients.html',
})
export class PatientsPage {
  private readonly recordsApi = inject(MedicalRecordsService);
  private readonly patientsApi = inject(PatientsService);
  private readonly router = inject(Router);
  readonly age = age;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly patients = signal<MedicalRecordDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
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

  readonly visible = computed(() => {
    const text = normalize(this.search());
    if (!text) return this.patients();
    return this.patients().filter((patient) =>
      normalize(
        `${patient.firstName} ${patient.lastName} ${patient.documentNumber} ${patient.phone} ${patient.healthInsurance ?? ''} ${patient.memberNumber ?? ''} ${patient.emergencyContact ?? ''} ${patient.emergencyPhone ?? ''}`,
      ).includes(text),
    );
  });

  constructor() {
    void firstValueFrom(this.recordsApi.medicalRecords())
      .then((list) => this.patients.set(list))
      .catch((error) => this.error.set(errorMessage(error)))
      .finally(() => this.loading.set(false));
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

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '');
}
