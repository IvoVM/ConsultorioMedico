import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentTypeDto, BillingService, FeeDto, OrganizationService, errorMessage } from 'sdk';
import { FechaPipe, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { firstValueFrom } from 'rxjs';

type FeeRow = { type: AppointmentTypeDto; fee: FeeDto | null };

@Component({
  imports: [FormsModule, FechaPipe, ScreenNote, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './fees.html',
})
export class FeesPage {
  private readonly organization = inject(OrganizationService);
  private readonly billing = inject(BillingService);
  readonly appointmentTypes = signal<AppointmentTypeDto[]>([]);
  readonly fees = signal<FeeDto[]>([]);
  readonly rows = computed(() => {
    const current = new Map(this.fees().map((fee) => [fee.appointmentTypeId, fee]));
    return this.appointmentTypes()
      .map((type) => ({ type, fee: current.get(type.id) ?? null }))
      .sort((a, b) => a.type.name.localeCompare(b.type.name, 'es'));
  });
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly editing = signal<FeeRow | null>(null);
  readonly draft = signal<number | null>(null);
  readonly priceError = computed(() => priceError(this.draft(), this.editing()?.fee?.amount ?? null));

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
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  open(row: FeeRow) {
    this.error.set('');
    this.draft.set(row.fee?.amount ?? null);
    this.editing.set(row);
  }

  close() {
    if (this.saving()) return;
    this.editing.set(null);
    this.error.set('');
  }

  onDraft(value: number | string | null) {
    if (value === null || value === '') {
      this.draft.set(null);
      return;
    }
    const amount = typeof value === 'number' ? value : Number(value);
    this.draft.set(Number.isNaN(amount) ? null : amount);
  }

  async save() {
    const row = this.editing();
    const amount = this.draft();
    const message = priceError(amount, row?.fee?.amount ?? null);
    if (!row || amount === null || message) {
      this.error.set(message);
      return;
    }
    this.saving.set(true);
    this.error.set('');
    try {
      await firstValueFrom(
        this.billing.createFee({
          appointmentTypeId: row.type.id,
          amount,
          effectiveFrom: new Date().toISOString().slice(0, 10),
        }),
      );
      this.editing.set(null);
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}

function priceError(amount: number | null, current: number | null) {
  if (amount === null || Number.isNaN(amount)) return 'El valor no puede ser 0.';
  const rounded = Math.round(amount * 100) / 100;
  if (rounded < 0) return 'El valor no puede ser negativo.';
  if (rounded === 0) return 'El valor no puede ser 0.';
  if (current !== null && rounded === Math.round(current * 100) / 100) return 'El valor tiene que ser distinto al actual.';
  return '';
}
