import { Component, inject, signal } from '@angular/core';
import { BillingService, InvoiceDto, errorMessage } from 'sdk';
import { invoiceStatusLabels, paymentMethodLabels } from './models/invoice-labels';
import { UiBadge, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge],
  templateUrl: './invoices.html',
})
export class InvoicesPage {
  private readonly billing = inject(BillingService);
  readonly statusLabels = invoiceStatusLabels;
  readonly methodLabels = paymentMethodLabels;
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly error = signal('');

  constructor() {
    void firstValueFrom(this.billing.invoices())
      .then((list) => this.invoices.set(list))
      .catch((error) => this.error.set(errorMessage(error)));
  }
}
