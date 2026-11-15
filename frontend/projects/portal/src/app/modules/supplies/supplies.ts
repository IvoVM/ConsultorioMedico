import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SupplyDto, SupplyMovementDto, SupplyMovementKind, SuppliesService, errorMessage } from 'sdk';
import { FechaPipe, ToastService, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { supplyMovementLabels } from './models/supply-movement-labels';

const searchDelay = 300;

// Front desk's view of the supplies: what is on the shelf, what is running low, loading a delivery
// and seeing what moved each one. The catalog and the corrections after counting are the admin's.
@Component({
  imports: [FormsModule, FechaPipe, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSkeleton, UiTable],
  templateUrl: './supplies.html',
})
export class SuppliesPage {
  private readonly api = inject(SuppliesService);
  private readonly toasts = inject(ToastService);
  private searchTimer?: ReturnType<typeof setTimeout>;
  readonly kinds = supplyMovementLabels;
  readonly rows = signal<SupplyDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly query = signal('');
  readonly lowOnly = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly entry = signal<SupplyDto | null>(null);
  readonly quantity = signal<number | null>(null);
  readonly note = signal('');
  readonly saving = signal(false);
  readonly entryError = signal('');
  readonly history = signal<SupplyDto | null>(null);
  readonly movements = signal<SupplyMovementDto[]>([]);
  readonly movementTotal = signal(0);
  readonly movementPage = signal(1);
  readonly movementLoading = signal(false);
  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly movementPages = computed(() => Math.max(1, Math.ceil(this.movementTotal() / this.pageSize())));

  constructor() {
    void this.load();
  }

  async load() {
    this.loading.set(true);
    this.error.set('');
    try {
      const result = await firstValueFrom(
        this.api.suppliesPage({ page: this.page(), q: this.query() || undefined, low: this.lowOnly() || undefined }),
      );
      this.rows.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
      this.pageSize.set(result.pageSize);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  onSearch(value: string) {
    this.query.set(value);
    this.page.set(1);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => void this.load(), searchDelay);
  }

  setLow(value: boolean) {
    this.lowOnly.set(value);
    this.page.set(1);
    void this.load();
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  amount(value: number, unit: string) {
    return `${value.toLocaleString('es-AR', { maximumFractionDigits: 2 })} ${unit}`;
  }

  signed(movement: SupplyMovementDto, unit: string) {
    const sign = movement.quantity > 0 ? '+' : '';
    return `${sign}${this.amount(movement.quantity, unit)}`;
  }

  movementTone(kind: SupplyMovementKind) {
    return kind === SupplyMovementKind.Entry ? 'ok' : kind === SupplyMovementKind.Adjustment ? 'warn' : 'muted';
  }

  openEntry(item: SupplyDto) {
    this.entry.set(item);
    this.quantity.set(null);
    this.note.set('');
    this.entryError.set('');
  }

  async saveEntry() {
    const item = this.entry();
    const quantity = this.quantity();
    if (!item) return;
    if (quantity === null || !(quantity > 0)) {
      this.entryError.set('Escribí cuánto ingresó.');
      return;
    }
    this.saving.set(true);
    this.entryError.set('');
    try {
      const saved = await firstValueFrom(this.api.addEntry(item.id, { quantity, note: this.note().trim() || null }));
      this.toasts.success('Ingreso cargado', `${saved.name}: quedan ${this.amount(saved.stock, saved.unit)}.`);
      this.entry.set(null);
      await this.load();
    } catch (error) {
      this.entryError.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  async openHistory(item: SupplyDto) {
    this.history.set(item);
    this.movementPage.set(1);
    await this.loadMovements();
  }

  async goMovements(page: number) {
    this.movementPage.set(page);
    await this.loadMovements();
  }

  private async loadMovements() {
    const item = this.history();
    if (!item) return;
    this.movementLoading.set(true);
    try {
      const result = await firstValueFrom(this.api.movements(item.id, { page: this.movementPage() }));
      this.movements.set(result.items);
      this.movementTotal.set(result.total);
      this.movementPage.set(result.page);
    } catch (error) {
      this.toasts.error('No se pudieron traer los movimientos', errorMessage(error));
    } finally {
      this.movementLoading.set(false);
    }
  }
}
