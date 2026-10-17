import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BillingService, FeeRowDto, errorMessage } from 'sdk';
import { FechaPipe, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, FechaPipe, ScreenNote, SortCol, TablePager, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './fees.html',
})
export class FeesPage {
  private readonly billing = inject(BillingService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly rows = signal<FeeRowDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly editing = signal<FeeRowDto | null>(null);
  readonly draft = signal<number | null>(null);
  readonly priceError = computed(() => priceError(this.draft(), this.editing()?.amount ?? null));

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const result = await firstValueFrom(this.billing.feesPage(listParams(this.page(), this.sort(), this.query())));
      this.rows.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  sortBy(key: string) {
    this.sort.update((current) => nextSort(current, key));
    this.page.set(1);
    void this.load();
  }

  onSearch(value: string) {
    this.query.set(value);
    this.page.set(1);
    queueSearch(this.searchHost, () => void this.load());
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  open(row: FeeRowDto) {
    this.error.set('');
    this.draft.set(row.amount ?? null);
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
    const message = priceError(amount, row?.amount ?? null);
    if (!row || amount === null || message) {
      this.error.set(message);
      return;
    }
    this.saving.set(true);
    this.error.set('');
    try {
      await firstValueFrom(
        this.billing.createFee({
          appointmentTypeId: row.appointmentTypeId,
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
