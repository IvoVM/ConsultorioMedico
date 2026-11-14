import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BillingService, CardBrand, CardSettlementDto, ReconciliationDto, SettlementLineDto, errorMessage } from 'sdk';
import { FechaPipe, SelectOption, ToastService, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable, formatMoney } from 'ui';
import { firstValueFrom } from 'rxjs';
import { cardBrandLabels } from '../invoices/models/invoice-labels';
import { cardBrandOptions } from '../invoices/models/invoice-view';

function isoDay(offset: number) {
  const date = new Date();
  date.setDate(date.getDate() + offset);
  return date.toISOString().slice(0, 10);
}

// Card reconciliation: bring the processor's settlement for a brand and some days, see each charge matched
// to the payment taken at the desk with the fee kept and the day it is paid, and look into what did not match.
@Component({
  imports: [FormsModule, FechaPipe, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './reconciliation.html',
})
export class ReconciliationPage {
  private readonly billing = inject(BillingService);
  private readonly toasts = inject(ToastService);
  readonly money = formatMoney;
  readonly brandLabels = cardBrandLabels;
  readonly brandOptions = cardBrandOptions;
  readonly data = signal<ReconciliationDto | null>(null);
  readonly loading = signal(true);
  readonly importing = signal(false);
  readonly error = signal('');
  readonly brand = signal<string>(CardBrand.Visa);
  readonly from = signal(isoDay(-14));
  readonly to = signal(isoDay(-1));
  readonly viewing = signal<CardSettlementDto | null>(null);
  readonly matching = signal<SettlementLineDto | null>(null);
  readonly receiptId = signal('');
  readonly matchError = signal('');
  readonly unmatched = computed(() => this.data()?.settlements.reduce((sum, item) => sum + item.lines.filter((line) => !line.receiptId).length, 0) ?? 0);
  readonly receiptOptions = computed((): SelectOption[] => {
    const line = this.matching();
    const settlement = this.viewing();
    return (this.data()?.pending ?? [])
      .filter((item) => !settlement || item.brand === settlement.brand)
      .filter((item) => !line || item.amount === line.gross)
      .map((item) => ({ value: item.id, label: `${item.patient} · lote ${item.batch} cupón ${item.coupon} · ${formatMoney(item.amount)}` }));
  });

  constructor() {
    void this.load();
  }

  matched(settlement: CardSettlementDto) {
    return settlement.lines.filter((line) => line.receiptId).length;
  }

  async bring() {
    this.importing.set(true);
    this.error.set('');
    try {
      const before = this.data()?.pending.length ?? 0;
      const result = await firstValueFrom(this.billing.importSettlement({ brand: this.brand() as CardBrand, from: this.from(), to: this.to() }));
      this.data.set(result);
      const lines = result.settlements[0]?.lines.length ?? 0;
      this.toasts.success('Liquidación traída', `${lines} cargos; se conciliaron ${before - result.pending.length}.`);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.importing.set(false);
    }
  }

  startMatch(line: SettlementLineDto) {
    this.matchError.set('');
    this.receiptId.set('');
    this.matching.set(line);
  }

  async match() {
    const line = this.matching();
    if (!line || !this.receiptId()) return;
    try {
      const result = await firstValueFrom(this.billing.matchSettlementLine(line.id, { receiptId: this.receiptId() }));
      this.data.set(result);
      this.viewing.set(result.settlements.find((item) => item.id === this.viewing()?.id) ?? null);
      this.matching.set(null);
    } catch (error) {
      this.matchError.set(errorMessage(error));
    }
  }

  private async load() {
    try {
      this.data.set(await firstValueFrom(this.billing.reconciliation()));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
