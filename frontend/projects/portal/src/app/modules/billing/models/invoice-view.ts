import { HttpErrorResponse } from '@angular/common/http';
import { BillingService, InvoiceDto, InvoiceStatus, PaymentMethod, errorMessage } from 'sdk';
import { InvoiceSource, InvoiceView, SelectOption } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { invoiceStatusLabels, paymentMethodLabels, voucherTypeLabels } from './invoice-labels';

export const paymentOptions: SelectOption[] = Object.values(PaymentMethod).map((value) => ({ value, label: paymentMethodLabels[value] }));

export function voucherNumber(pointOfSale?: number | null, number?: number | null) {
  return pointOfSale != null && number != null ? `${String(pointOfSale).padStart(4, '0')}-${String(number).padStart(8, '0')}` : '';
}

export function invoiceTitle(invoice: InvoiceDto) {
  return invoice.voucherType ? `${voucherTypeLabels[invoice.voucherType]} ${voucherNumber(invoice.pointOfSale, invoice.number)}` : 'Comprobante sin autorizar';
}

export function invoiceTone(status: InvoiceStatus) {
  return status === InvoiceStatus.Paid ? 'ok' : status === InvoiceStatus.Voided ? 'muted' : 'warn';
}

// Front desk acts on the invoice; a patient only reads it and downloads the PDF.
export function invoiceView(invoice: InvoiceDto, options: { staff: boolean; fiscalEnabled: boolean }): InvoiceView {
  const live = invoice.status !== InvoiceStatus.Voided;
  return {
    heading: invoiceTitle(invoice),
    status: invoiceStatusLabels[invoice.status],
    tone: invoiceTone(invoice.status),
    patient: invoice.patient,
    patientDocument: invoice.patientDocument,
    professional: invoice.professional,
    appointmentStart: invoice.appointmentStart,
    items: invoice.items,
    subtotal: invoice.subtotal,
    discount: invoice.discount,
    total: invoice.total,
    insurer: invoice.insurer ?? null,
    insurerAmount: invoice.insurerAmount,
    patientAmount: invoice.patientAmount,
    paid: invoice.paid,
    balance: invoice.balance,
    payments: invoice.payments.map((payment) => ({ ...payment, method: paymentMethodLabels[payment.method] })),
    cae: invoice.cae ?? null,
    caeDueDate: invoice.caeDueDate ?? null,
    fiscalSimulated: invoice.fiscalSimulated,
    voidReason: invoice.voidReason ?? null,
    creditNote: invoice.creditNoteType
      ? `${voucherTypeLabels[invoice.creditNoteType]} ${voucherNumber(invoice.pointOfSale, invoice.creditNoteNumber)} · CAE ${invoice.creditNoteCae}`
      : null,
    editable: options.staff && invoice.editable,
    canPay: options.staff && live && invoice.balance > 0,
    canAuthorize: options.staff && options.fiscalEnabled && live && invoice.number == null && invoice.total > 0,
    canVoid: options.staff && live,
  };
}

async function call<T>(request: Observable<T>) {
  try {
    return await firstValueFrom(request);
  } catch (error) {
    throw new Error(errorMessage(error));
  }
}

export async function downloadInvoicePdf(billing: BillingService, invoice: InvoiceDto) {
  let blob: Blob;
  try {
    blob = await firstValueFrom(billing.invoicePdf(invoice.id));
  } catch (error) {
    // The PDF endpoint answers errors as JSON inside a blob.
    const body = error instanceof HttpErrorResponse && error.error instanceof Blob ? await error.error.text() : '';
    throw new Error(body ? errorMessage({ error: JSON.parse(body) }) : errorMessage(error));
  }
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = invoice.number != null ? `comprobante-${voucherNumber(invoice.pointOfSale, invoice.number)}.pdf` : `comprobante-${invoice.id.slice(0, 8)}.pdf`;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// Each action reports the updated invoice back so the screen can refresh its list.
export function invoiceSource(billing: BillingService, invoice: InvoiceDto, updated: (invoice: InvoiceDto) => void | Promise<void>): InvoiceSource {
  return {
    save: async (edit) => updated(await call(billing.updateInvoice(invoice.id, edit))),
    pay: async (amount, method) => updated(await call(billing.payInvoice(invoice.id, { amount, method: method as PaymentMethod }))),
    authorize: async () => updated(await call(billing.authorizeInvoice(invoice.id))),
    void: async (reason) => updated(await call(billing.voidInvoice(invoice.id, { reason }))),
    pdf: () => downloadInvoicePdf(billing, invoice),
  };
}
