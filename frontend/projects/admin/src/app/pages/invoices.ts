import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, InvoiceDto, errorMessage, invoiceStatusLabels, paymentMethodLabels } from 'sdk';
import { UiBadge, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge],
  templateUrl: './invoices.html',
})
export class InvoicesPage {
  private readonly api = inject(ClinicaClient);
  readonly statusLabels = invoiceStatusLabels;
  readonly methodLabels = paymentMethodLabels;
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly error = signal('');

  constructor() {
    void firstValueFrom(this.api.invoices())
      .then((list) => this.invoices.set(list))
      .catch((error) => this.error.set(errorMessage(error)));
  }
}
