import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ClinicalHistoryDto, ClinicalService, DiagnosisDto, TenantRole, clinicaSession, errorMessage } from 'sdk';
import { FechaPipe, TabItem, UiEmpty, UiSkeleton, UiTabs } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FechaPipe, UiEmpty, UiSkeleton, UiTabs],
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
  readonly tabs = computed((): TabItem[] =>
    (this.history()?.specialties ?? []).map((item) => ({ key: item.id, label: item.name, count: item.encounters })),
  );
  patientId = '';

  constructor() {
    const paciente = this.route.snapshot.queryParamMap.get('paciente');
    if (paciente) this.patientId = paciente;
    if (clinicaSession.role() === TenantRole.Patient || paciente) void this.load();
  }

  async load(specialtyId?: string) {
    this.error.set('');
    this.loading.set(true);
    try {
      const patient = clinicaSession.role() === TenantRole.Patient ? {} : { patientId: this.patientId };
      const query = specialtyId ? { ...patient, specialtyId } : patient;
      this.history.set(await firstValueFrom(this.clinical.clinicalHistory(query)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  pick(specialtyId: string) {
    if (specialtyId !== this.history()?.specialty?.id) void this.load(specialtyId);
  }

  diagnosesText(diagnoses: DiagnosisDto[]) {
    return diagnoses.map((d) => `${d.code} ${d.name}`).join(', ');
  }
}
