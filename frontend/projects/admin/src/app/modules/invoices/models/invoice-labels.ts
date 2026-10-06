import { InvoiceStatus, PaymentMethod } from 'sdk';

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  Cash: 'Efectivo',
  Transfer: 'Transferencia',
  Card: 'Tarjeta',
};

export const invoiceStatusLabels: Record<InvoiceStatus, string> = {
  Pending: 'Pendiente',
  Paid: 'Pagado',
  Voided: 'Anulado',
};
