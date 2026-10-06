import { InvoiceStatus } from 'sdk';

export const invoiceStatusLabels: Record<InvoiceStatus, string> = {
  Pending: 'Pendiente',
  Paid: 'Pagado',
  Voided: 'Anulado',
};
