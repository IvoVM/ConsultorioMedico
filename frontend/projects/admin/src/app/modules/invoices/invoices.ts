import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BillingService, InvoiceDto, errorMessage } from 'sdk';
import { invoiceStatusLabels, paymentMethodLabels } from './models/invoice-labels';
import { UiBadge, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, SortCol, TablePager, UiBadge, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './invoices.html',
})
export class InvoicesPage {
  private readonly billing = inject(BillingService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly statusLabels = invoiceStatusLabels;
  readonly methodLabels = paymentMethodLabels;
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'patient', dir: 'asc' });

  constructor() {
    void this.load();
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

  private async load() {
    try {
      const result = await firstValueFrom(this.billing.invoicesPage(listParams(this.page(), this.sort(), this.query())));
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
