import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SupplyDto, SupplyMovementDto, SupplyMovementKind, SuppliesService, errorMessage } from 'sdk';
import { FechaPipe, ToastService, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { supplyMovementLabels } from './models/supply-movement-labels';

type Editing = { id: string | null; name: string; unit: string; minStock: number };
type Counting = { supply: SupplyDto; stock: number | null; reason: string };

// The supplies catalog and its corrections: the admin loads each supply, its unit and its minimum, and fixes
// the stock after counting the shelf, with the reason. Front desk loads the deliveries from the portal.
@Component({
  imports: [FormsModule, FechaPipe, SortCol, TablePager, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSkeleton, UiTable],
  templateUrl: './supplies.html',
})
export class SuppliesPage {
  private readonly api = inject(SuppliesService);
  private readonly toasts = inject(ToastService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly kinds = supplyMovementLabels;
  readonly rows = signal<SupplyDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly query = signal('');
  readonly lowOnly = signal(false);
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  readonly loading = signal(true);
  readonly error = signal('');
  readonly editing = signal<Editing | null>(null);
  readonly counting = signal<Counting | null>(null);
  readonly saving = signal(false);
  readonly dialogError = signal('');
  readonly busy = signal('');
  readonly history = signal<SupplyDto | null>(null);
  readonly movements = signal<SupplyMovementDto[]>([]);
  readonly movementTotal = signal(0);
  readonly movementPage = signal(1);

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const result = await firstValueFrom(
        this.api.suppliesPage({ ...listParams(this.page(), this.sort(), this.query()), low: this.lowOnly() || undefined }),
      );
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
    return `${movement.quantity > 0 ? '+' : ''}${this.amount(movement.quantity, unit)}`;
  }

  movementTone(kind: SupplyMovementKind) {
    return kind === SupplyMovementKind.Entry ? 'ok' : kind === SupplyMovementKind.Adjustment ? 'warn' : 'muted';
  }

  openEdit(item?: SupplyDto) {
    this.dialogError.set('');
    this.editing.set({ id: item?.id ?? null, name: item?.name ?? '', unit: item?.unit ?? 'unidad', minStock: item?.minStock ?? 0 });
  }

  edit(patch: Partial<Editing>) {
    this.editing.update((current) => current && { ...current, ...patch });
  }

  async saveEdit() {
    const form = this.editing();
    if (!form) return;
    const body = { name: form.name.trim(), unit: form.unit.trim(), minStock: Number(form.minStock) || 0 };
    await this.dialog(async () => {
      await firstValueFrom(form.id ? this.api.updateSupply(form.id, body) : this.api.createSupply(body));
      this.editing.set(null);
    });
  }

  openCount(supply: SupplyDto) {
    this.dialogError.set('');
    this.counting.set({ supply, stock: supply.stock, reason: '' });
  }

  count(patch: Partial<Counting>) {
    this.counting.update((current) => current && { ...current, ...patch });
  }

  async saveCount() {
    const form = this.counting();
    if (!form) return;
    if (form.stock === null || form.stock < 0) {
      this.dialogError.set('Escribí cuánto hay en el estante.');
      return;
    }
    await this.dialog(async () => {
      const saved = await firstValueFrom(this.api.count(form.supply.id, { stock: form.stock!, reason: form.reason.trim() }));
      this.toasts.success('Stock corregido', `${saved.name}: ${this.amount(saved.stock, saved.unit)}.`);
      this.counting.set(null);
    });
  }

  async toggle(item: SupplyDto) {
    this.busy.set(item.id);
    try {
      await firstValueFrom(this.api.setSupplyActive(item.id, !item.isActive));
      await this.load();
    } catch (error) {
      this.toasts.error(`No se pudo ${item.isActive ? 'dar de baja' : 'reactivar'}`, errorMessage(error));
    } finally {
      this.busy.set('');
    }
  }

  async openHistory(item: SupplyDto) {
    this.history.set(item);
    await this.goMovements(1);
  }

  async goMovements(page: number) {
    const item = this.history();
    if (!item) return;
    try {
      const result = await firstValueFrom(this.api.movements(item.id, { page }));
      this.movements.set(result.items);
      this.movementTotal.set(result.total);
      this.movementPage.set(result.page);
    } catch (error) {
      this.toasts.error('No se pudieron traer los movimientos', errorMessage(error));
    }
  }

  private async dialog(action: () => Promise<void>) {
    this.saving.set(true);
    this.dialogError.set('');
    try {
      await action();
      await this.load();
    } catch (error) {
      this.dialogError.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
