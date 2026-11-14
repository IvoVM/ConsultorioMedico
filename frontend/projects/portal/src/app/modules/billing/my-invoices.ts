import { Component, computed, inject, signal } from '@angular/core';
import { BillingService, InvoiceDto, InvoiceStatus, errorMessage } from 'sdk';
import { FechaPipe, ToastService, UiBadge, UiButton, UiEmpty, UiInvoice, UiSkeleton, formatMoney } from 'ui';
import { firstValueFrom } from 'rxjs';
import { invoiceStatusLabels } from './models/invoice-labels';
import { downloadInvoicePdf, invoiceSource, invoiceTitle, invoiceTone, invoiceView } from './models/invoice-view';

// The patient's own invoices: what each visit cost, what they paid and what they still owe, with the PDF to keep.
@Component({
  imports: [FechaPipe, UiBadge, UiButton, UiEmpty, UiInvoice, UiSkeleton],
  templateUrl: './my-invoices.html',
})
export class MyInvoicesPage {
  private readonly billing = inject(BillingService);
  private readonly toasts = inject(ToastService);
  readonly status = InvoiceStatus;
  readonly statusLabels = invoiceStatusLabels;
  readonly title = invoiceTitle;
  readonly tone = invoiceTone;
  readonly money = formatMoney;
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly downloading = signal('');
  readonly opened = signal<InvoiceDto | null>(null);
  readonly openedView = computed(() => {
    const invoice = this.opened();
    return invoice ? invoiceView(invoice, { staff: false, fiscalEnabled: false }) : null;
  });
  readonly openedSource = computed(() => {
    const invoice = this.opened();
    return invoice ? invoiceSource(this.billing, invoice, () => undefined) : null;
  });
  readonly owed = computed(() => this.invoices().reduce((sum, invoice) => sum + invoice.balance, 0));

  constructor() {
    void firstValueFrom(this.billing.myInvoices())
      .then((list) => this.invoices.set(list))
      .catch((error) => this.error.set(errorMessage(error)))
      .finally(() => this.loading.set(false));
  }

  async download(invoice: InvoiceDto) {
    this.downloading.set(invoice.id);
    try {
      await downloadInvoicePdf(this.billing, invoice);
    } catch (error) {
      this.toasts.error('No se pudo descargar', error instanceof Error ? error.message : '');
    } finally {
      this.downloading.set('');
    }
  }
}
