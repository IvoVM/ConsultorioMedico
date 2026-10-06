import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, PrescriptionDto, errorMessage } from 'sdk';
import { UiButton, UiEmpty } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiButton, UiEmpty],
  templateUrl: './prescriptions.html',
})
export class PrescriptionsPage {
  private readonly api = inject(ClinicaClient);
  readonly prescriptions = signal<PrescriptionDto[]>([]);
  readonly error = signal('');

  constructor() {
    void firstValueFrom(this.api.myPrescriptions())
      .then((list) => this.prescriptions.set(list))
      .catch((error) => this.error.set(errorMessage(error)));
  }

  print() {
    window.print();
  }
}
