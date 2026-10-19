import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ClinicalHistoryDto, ClinicalService, DiagnosisDto, TenantRole, clinicaSession, errorMessage } from 'sdk';
import { FechaPipe, UiEmpty, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FechaPipe, UiEmpty, UiSkeleton],
  templateUrl: './clinical-history.html',
})
export class ClinicalHistoryPage {
  private readonly clinical = inject(ClinicalService);
  private readonly route = inject(ActivatedRoute);
  readonly session = clinicaSession;
  readonly roles = TenantRole;
  readonly history = signal<ClinicalHistoryDto | null>(null);
  readonly error = signal('');
  readonly loading = signal(false);
  patientId = '';

  constructor() {
    const paciente = this.route.snapshot.queryParamMap.get('paciente');
    if (paciente) this.patientId = paciente;
    if (clinicaSession.role() === TenantRole.Patient || paciente) void this.load();
  }

  async load() {
    this.error.set('');
    this.loading.set(true);
    try {
      const query = clinicaSession.role() === TenantRole.Patient ? {} : { patientId: this.patientId };
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
