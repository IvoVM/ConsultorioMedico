import { FiscalVoucherType, InvoiceStatus, PaymentMethod } from 'sdk';

export const invoiceStatusLabels: Record<InvoiceStatus, string> = {
  [InvoiceStatus.Pending]: 'Pendiente',
  [InvoiceStatus.PartiallyPaid]: 'Pago parcial',
  [InvoiceStatus.Paid]: 'Pagado',
  [InvoiceStatus.Voided]: 'Anulado',
};

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  [PaymentMethod.Cash]: 'Efectivo',
  [PaymentMethod.Transfer]: 'Transferencia',
  [PaymentMethod.Card]: 'Tarjeta',
};

export const voucherTypeLabels: Record<FiscalVoucherType, string> = {
  [FiscalVoucherType.FacturaB]: 'Factura B',
  [FiscalVoucherType.FacturaC]: 'Factura C',
  [FiscalVoucherType.NotaCreditoB]: 'Nota de crédito B',
  [FiscalVoucherType.NotaCreditoC]: 'Nota de crédito C',
};
