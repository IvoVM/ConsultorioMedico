import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, ClinicalHistoryDto, DiagnosisDto, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiEmpty],
  templateUrl: './clinical-history.html',
})
export class ClinicalHistoryPage {
  private readonly api = inject(ClinicaClient);
  readonly session = clinicaSession;
  readonly history = signal<ClinicalHistoryDto | null>(null);
  readonly error = signal('');
  patientId = '';

  constructor() {
    if (clinicaSession.role() === 'Patient') void this.load();
  }

  async load() {
    this.error.set('');
    try {
      const query = clinicaSession.role() === 'Patient' ? {} : { patientId: this.patientId };
      this.history.set(await firstValueFrom(this.api.clinicalHistory(query)));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  diagnosesText(diagnoses: DiagnosisDto[]) {
    return diagnoses.map((d) => `${d.code} ${d.name}`).join(', ');
  }
}
