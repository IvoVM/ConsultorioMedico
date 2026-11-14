import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BillingService, ClinicService, InvoiceDto, InvoiceStatus, errorMessage } from 'sdk';
import { invoiceStatusLabels, voucherTypeLabels } from './models/invoice-labels';
import { InvoiceTab, invoiceTabLabels } from './models/invoice-tab';
import { invoiceSource, invoiceTone, invoiceView, paymentOptions, voucherNumber } from './models/invoice-view';
import { FechaPipe, SelectOption, TabItem, UiBadge, UiButton, UiEmpty, UiField, UiInvoice, UiSelect, UiSkeleton, UiTable, UiTabs, formatMoney } from 'ui';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';

// Every status still in force, plus an empty value for all of them; the status goes to the API as its enum value.
const allStatuses = '';

@Component({
  imports: [FormsModule, FechaPipe, SortCol, TablePager, UiBadge, UiButton, UiEmpty, UiField, UiInvoice, UiSelect, UiSkeleton, UiTable, UiTabs],
  templateUrl: './invoices.html',
})
export class InvoicesPage {
  private readonly billing = inject(BillingService);
  private readonly clinic = inject(ClinicService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly statusLabels = invoiceStatusLabels;
  readonly tone = invoiceTone;
  readonly number = voucherNumber;
  readonly money = formatMoney;
  readonly paymentOptions = paymentOptions;
  readonly statusOptions: SelectOption[] = [
    { value: allStatuses, label: 'Todos los estados' },
    ...Object.values(InvoiceStatus)
      .filter((value) => value !== InvoiceStatus.Voided)
      .map((value) => ({ value, label: invoiceStatusLabels[value] })),
  ];
  readonly invoiceTab = InvoiceTab;
  readonly tabs: TabItem[] = [
    { key: InvoiceTab.Vigentes, label: invoiceTabLabels[InvoiceTab.Vigentes], icon: 'receipt_long' },
    { key: InvoiceTab.Anulados, label: invoiceTabLabels[InvoiceTab.Anulados], icon: 'block' },
  ];
  readonly tab = signal<string>(InvoiceTab.Vigentes);
  readonly voided = computed(() => this.tab() === InvoiceTab.Anulados);
  readonly voucherLabels = voucherTypeLabels;
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly query = signal('');
  readonly status = signal<string>(allStatuses);
  readonly sort = signal<SortState>({ key: 'date', dir: 'desc' });
  readonly fiscalEnabled = signal(false);
  readonly opened = signal<InvoiceDto | null>(null);
  readonly openedView = computed(() => {
    const invoice = this.opened();
    return invoice ? invoiceView(invoice, { staff: true, fiscalEnabled: this.fiscalEnabled() }) : null;
  });
  readonly openedSource = computed(() => {
    const invoice = this.opened();
    return invoice
      ? invoiceSource(this.billing, invoice, async (updated) => {
          this.opened.set(updated);
          await this.load();
        })
      : null;
  });

  constructor() {
    void this.load();
    void firstValueFrom(this.clinic.clinicProfile())
      .then((profile) => this.fiscalEnabled.set(profile.fiscalEnabled))
      .catch(() => undefined);
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

  setTab(value: string) {
    this.tab.set(value);
    this.status.set(allStatuses);
    this.sort.set({ key: value === InvoiceTab.Anulados ? 'voided' : 'date', dir: 'desc' });
    this.page.set(1);
    this.loading.set(true);
    void this.load();
  }

  onStatus(value: string) {
    this.status.set(value);
    this.page.set(1);
    void this.load();
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  private async load() {
    try {
      const result = await firstValueFrom(this.billing.invoicesPage({ ...listParams(this.page(), this.sort(), this.query(), this.status()), voided: this.voided() || undefined }));
      this.invoices.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
