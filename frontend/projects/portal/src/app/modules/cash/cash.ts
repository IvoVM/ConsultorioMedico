import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BillingService, CashMovementKind, CashSessionDto, CashSessionStatus, LocationDto, OrganizationService, PaymentMethod, errorMessage } from 'sdk';
import { FechaPipe, SelectOption, ToastService, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable, formatMoney } from 'ui';
import { firstValueFrom } from 'rxjs';
import { cashMovementKindLabels, cashSessionStatusLabels, paymentMethodLabels } from '../billing/models/invoice-labels';
import { paymentOptions } from '../billing/models/invoice-view';
import { CashDialog } from './models/cash-dialog';

// The register of each site: open it with the cash in the drawer, see every payment, refund and hand-made
// entry as it happens, and close it counting the cash against what should be there.
@Component({
  imports: [FormsModule, FechaPipe, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './cash.html',
})
export class CashPage {
  private readonly billing = inject(BillingService);
  private readonly organization = inject(OrganizationService);
  private readonly toasts = inject(ToastService);
  readonly money = formatMoney;
  readonly dialogs = CashDialog;
  readonly kind = CashMovementKind;
  readonly sessionStatus = CashSessionStatus;
  readonly kindLabels = cashMovementKindLabels;
  readonly statusLabels = cashSessionStatusLabels;
  readonly methodLabels = paymentMethodLabels;
  readonly methodOptions = paymentOptions;
  readonly locations = signal<LocationDto[]>([]);
  readonly locationId = signal('');
  readonly current = signal<CashSessionDto | null>(null);
  readonly history = signal<CashSessionDto[]>([]);
  readonly viewing = signal<CashSessionDto | null>(null);
  readonly dialog = signal<CashDialog | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly dialogError = signal('');
  readonly openingCash = signal<number | null>(0);
  readonly movementMethod = signal<string>(PaymentMethod.Cash);
  readonly movementAmount = signal<number | null>(null);
  readonly movementDescription = signal('');
  readonly countedCash = signal<number | null>(null);
  readonly notes = signal('');
  readonly locationOptions = computed((): SelectOption[] => this.locations().map((item) => ({ value: item.id, label: item.name })));
  readonly difference = computed(() => {
    const session = this.current();
    const counted = this.countedCash();
    return session && counted !== null && counted !== undefined ? Number(counted) - session.expectedCash : null;
  });

  constructor() {
    void this.start();
  }

  signed(kind: CashMovementKind, amount: number) {
    return kind === CashMovementKind.Payment || kind === CashMovementKind.Income ? amount : -amount;
  }

  differenceTone(value: number | null | undefined) {
    return !value ? 'ok' : 'warn';
  }

  async pickLocation(id: string) {
    this.locationId.set(id);
    await this.load();
  }

  openDialog(dialog: CashDialog) {
    this.dialogError.set('');
    this.movementAmount.set(null);
    this.movementDescription.set('');
    this.movementMethod.set(PaymentMethod.Cash);
    this.countedCash.set(this.current()?.expectedCash ?? null);
    this.notes.set('');
    this.dialog.set(dialog);
  }

  async view(session: CashSessionDto) {
    try {
      this.viewing.set(await firstValueFrom(this.billing.cashSession(session.id)));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async open() {
    await this.save(async () => {
      await firstValueFrom(this.billing.openCash({ locationId: this.locationId(), openingCash: Number(this.openingCash()) || 0 }));
      this.toasts.success('Caja abierta', 'Ya se puede cobrar en esta sede.');
    });
  }

  async addMovement() {
    const session = this.current();
    if (!session) return;
    const kind = this.dialog() === CashDialog.Income ? CashMovementKind.Income : CashMovementKind.Expense;
    await this.save(async () => {
      await firstValueFrom(
        this.billing.addCashMovement(session.id, {
          kind,
          method: this.movementMethod() as PaymentMethod,
          amount: Number(this.movementAmount()) || 0,
          description: this.movementDescription(),
        }),
      );
    });
  }

  async close() {
    const session = this.current();
    if (!session) return;
    await this.save(async () => {
      const closed = await firstValueFrom(this.billing.closeCash(session.id, { countedCash: Number(this.countedCash()) || 0, notes: this.notes() || null }));
      const difference = closed.difference ?? 0;
      this.toasts.success(
        'Caja cerrada',
        difference === 0 ? 'El efectivo contado coincide con lo esperado.' : `Diferencia de ${formatMoney(difference)} en efectivo.`,
      );
    });
  }

  private async save(action: () => Promise<void>) {
    this.saving.set(true);
    this.dialogError.set('');
    try {
      await action();
      this.dialog.set(null);
      await this.load();
    } catch (error) {
      this.dialogError.set(errorMessage(error));
      this.error.set(this.dialog() ? '' : errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  private async start() {
    try {
      const [locations, open] = await Promise.all([firstValueFrom(this.organization.locations()), firstValueFrom(this.billing.openCashSessions())]);
      const active = locations.filter((item) => item.isActive);
      this.locations.set(active);
      // Opens on a site whose register is open, which is where front desk is working.
      this.locationId.set(open[0]?.locationId ?? active[0]?.id ?? '');
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
      this.loading.set(false);
    }
  }

  private async load() {
    const locationId = this.locationId();
    if (!locationId) {
      this.loading.set(false);
      return;
    }
    this.error.set('');
    try {
      const sessions = await firstValueFrom(this.billing.cashSessions({ locationId }));
      const open = sessions.find((item) => item.status === CashSessionStatus.Open);
      this.current.set(open ? await firstValueFrom(this.billing.cashSession(open.id)) : null);
      this.history.set(sessions.filter((item) => item.id !== open?.id));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
