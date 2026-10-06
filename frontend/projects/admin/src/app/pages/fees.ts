import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentTypeDto, ClinicaClient, FeeDto, errorMessage } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './fees.html',
})
export class FeesPage {
  private readonly api = inject(ClinicaClient);
  readonly appointmentTypes = signal<AppointmentTypeDto[]>([]);
  readonly fees = signal<FeeDto[]>([]);
  readonly error = signal('');
  appointmentTypeId = '';
  amount = 0;
  effectiveFrom = new Date().toISOString().slice(0, 10);

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const [appointmentTypes, fees] = await Promise.all([firstValueFrom(this.api.appointmentTypes()), firstValueFrom(this.api.fees())]);
      this.appointmentTypes.set(appointmentTypes);
      this.fees.set(fees);
      this.appointmentTypeId ||= appointmentTypes[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async create() {
    try {
      await firstValueFrom(
        this.api.createFee({ appointmentTypeId: this.appointmentTypeId, amount: Number(this.amount), effectiveFrom: this.effectiveFrom }),
      );
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
