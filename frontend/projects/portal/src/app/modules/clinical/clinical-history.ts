import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicalHistoryDto, ClinicalService, DiagnosisDto, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiEmpty, UiSkeleton],
  templateUrl: './clinical-history.html',
})
export class ClinicalHistoryPage {
  private readonly clinical = inject(ClinicalService);
  readonly session = clinicaSession;
  readonly history = signal<ClinicalHistoryDto | null>(null);
  readonly error = signal('');
  readonly loading = signal(clinicaSession.role() === 'Patient');
  patientId = '';

  constructor() {
    if (clinicaSession.role() === 'Patient') void this.load();
  }

  async load() {
    this.error.set('');
    this.loading.set(true);
    try {
      const query = clinicaSession.role() === 'Patient' ? {} : { patientId: this.patientId };
      this.history.set(await firstValueFrom(this.clinical.clinicalHistory(query)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  diagnosesText(diagnoses: DiagnosisDto[]) {
    return diagnoses.map((d) => `${d.code} ${d.name}`).join(', ');
  }
}
