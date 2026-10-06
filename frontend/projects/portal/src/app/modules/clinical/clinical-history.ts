import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ClinicalHistoryDto, ClinicalService, DiagnosisDto, clinicaSession, errorMessage } from 'sdk';
import { FechaPipe, UiButton, UiEmpty, UiField, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, FechaPipe, UiButton, UiField, UiEmpty, UiSkeleton],
  templateUrl: './clinical-history.html',
})
export class ClinicalHistoryPage {
  private readonly clinical = inject(ClinicalService);
  private readonly route = inject(ActivatedRoute);
  readonly session = clinicaSession;
  readonly history = signal<ClinicalHistoryDto | null>(null);
  readonly error = signal('');
  readonly loading = signal(false);
  patientId = '';

  constructor() {
    const paciente = this.route.snapshot.queryParamMap.get('paciente');
    if (paciente) this.patientId = paciente;
    if (clinicaSession.role() === 'Patient' || paciente) void this.load();
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
