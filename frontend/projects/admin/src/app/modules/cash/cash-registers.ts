import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BillingService, CashMovementKind, CashSessionDto, CashSessionStatus, LocationDto, OrganizationService, errorMessage } from 'sdk';
import { FechaPipe, SelectOption, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable, formatMoney } from 'ui';
import { firstValueFrom } from 'rxjs';
import { cashMovementKindLabels, cashSessionStatusLabels, paymentMethodLabels } from '../invoices/models/invoice-labels';

const allLocations = '';

// Every register of every site, for the admin to follow how each shift closed: what came in by each
// method, the cash expected against the cash counted, and the movements behind it.
@Component({
  imports: [FormsModule, FechaPipe, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './cash-registers.html',
})
export class CashRegistersPage {
  private readonly billing = inject(BillingService);
  private readonly organization = inject(OrganizationService);
  readonly money = formatMoney;
  readonly statusLabels = cashSessionStatusLabels;
  readonly kindLabels = cashMovementKindLabels;
  readonly methodLabels = paymentMethodLabels;
  readonly open = CashSessionStatus.Open;
  readonly locations = signal<LocationDto[]>([]);
  readonly locationId = signal(allLocations);
  readonly sessions = signal<CashSessionDto[]>([]);
  readonly viewing = signal<CashSessionDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly locationOptions = computed((): SelectOption[] => [
    { value: allLocations, label: 'Todas las sedes' },
    ...this.locations().map((item) => ({ value: item.id, label: item.name })),
  ]);
  readonly shortfall = computed(() => this.sessions().reduce((sum, item) => sum + Math.min(0, item.difference ?? 0), 0));

  constructor() {
    void firstValueFrom(this.organization.locations()).then((list) => this.locations.set(list)).catch(() => undefined);
    void this.load();
  }

  signed(kind: CashMovementKind, amount: number) {
    return kind === CashMovementKind.Payment || kind === CashMovementKind.Income ? amount : -amount;
  }

  total(session: CashSessionDto) {
    return session.totals.reduce((sum, item) => sum + item.net, 0);
  }

  differenceTone(value: number | null | undefined) {
    return !value ? 'ok' : 'warn';
  }

  async pick(id: string) {
    this.locationId.set(id);
    await this.load();
  }

  async view(session: CashSessionDto) {
    try {
      this.viewing.set(await firstValueFrom(this.billing.cashSession(session.id)));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  private async load() {
    this.error.set('');
    try {
      this.sessions.set(await firstValueFrom(this.billing.cashSessions({ locationId: this.locationId() || undefined })));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
