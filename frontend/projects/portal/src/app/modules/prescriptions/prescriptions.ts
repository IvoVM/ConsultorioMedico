import { Component, inject, signal } from '@angular/core';
import { ClinicalService, PrescriptionDto, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiButton, UiEmpty, UiSkeleton],
  templateUrl: './prescriptions.html',
})
export class PrescriptionsPage {
  private readonly clinical = inject(ClinicalService);
  readonly prescriptions = signal<PrescriptionDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);

  constructor() {
    void firstValueFrom(this.clinical.myPrescriptions())
      .then((list) => this.prescriptions.set(list))
      .catch((error) => this.error.set(errorMessage(error)))
      .finally(() => this.loading.set(false));
  }

  print() {
    window.print();
  }
}
