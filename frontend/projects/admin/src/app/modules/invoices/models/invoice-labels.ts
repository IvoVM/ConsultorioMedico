import { AccountMovementKind, CardBrand, CashMovementKind, CashSessionStatus, FiscalVoucherType, InstallmentStatus, InvoiceStatus, PaymentMethod } from 'sdk';

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

export const cardBrandLabels: Record<CardBrand, string> = {
  [CardBrand.Visa]: 'Visa',
  [CardBrand.Mastercard]: 'Mastercard',
  [CardBrand.Amex]: 'American Express',
  [CardBrand.Cabal]: 'Cabal',
  [CardBrand.Naranja]: 'Naranja',
};

export const installmentStatusLabels: Record<InstallmentStatus, string> = {
  [InstallmentStatus.Pending]: 'A vencer',
  [InstallmentStatus.Overdue]: 'Vencida',
  [InstallmentStatus.Paid]: 'Pagada',
};

export const cashMovementKindLabels: Record<CashMovementKind, string> = {
  [CashMovementKind.Payment]: 'Cobro',
  [CashMovementKind.Refund]: 'Devolución',
  [CashMovementKind.Income]: 'Ingreso',
  [CashMovementKind.Expense]: 'Egreso',
};

export const cashSessionStatusLabels: Record<CashSessionStatus, string> = {
  [CashSessionStatus.Open]: 'Abierta',
  [CashSessionStatus.Closed]: 'Cerrada',
};

export const accountMovementKindLabels: Record<AccountMovementKind, string> = {
  [AccountMovementKind.Invoice]: 'Comprobante',
  [AccountMovementKind.Voided]: 'Anulación',
  [AccountMovementKind.Payment]: 'Pago',
  [AccountMovementKind.Refund]: 'Devolución',
};
