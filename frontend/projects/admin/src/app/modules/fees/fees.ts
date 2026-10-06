import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentTypeDto, BillingService, FeeDto, OrganizationService, errorMessage } from 'sdk';
import { UiButton, UiField, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiSkeleton, UiTable],
  templateUrl: './fees.html',
})
export class FeesPage {
  private readonly organization = inject(OrganizationService);
  private readonly billing = inject(BillingService);
  readonly appointmentTypes = signal<AppointmentTypeDto[]>([]);
  readonly fees = signal<FeeDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  appointmentTypeId = '';
  amount = 0;
  effectiveFrom = new Date().toISOString().slice(0, 10);

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const [appointmentTypes, fees] = await Promise.all([
        firstValueFrom(this.organization.appointmentTypes()),
        firstValueFrom(this.billing.fees()),
      ]);
      this.appointmentTypes.set(appointmentTypes);
      this.fees.set(fees);
      this.appointmentTypeId ||= appointmentTypes[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async create() {
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.billing.createFee({ appointmentTypeId: this.appointmentTypeId, amount: Number(this.amount), effectiveFrom: this.effectiveFrom }),
      );
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
