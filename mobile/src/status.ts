export enum AuthFrom {
  Solicitud = 'solicitud',
}

export enum BookingRequestStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Rescheduled = 'Rescheduled',
}

export enum AppointmentStatus {
  Booked = 'Booked',
  CheckedIn = 'CheckedIn',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow = 'NoShow',
  Rescheduled = 'Rescheduled',
}

// Which account to enter with when one document holds both a staff and a patient account.
export enum LoginAccount {
  Staff = 'Staff',
  Patient = 'Patient',
}

export enum InvoiceStatus {
  Pending = 'Pending',
  PartiallyPaid = 'PartiallyPaid',
  Paid = 'Paid',
  Voided = 'Voided',
}

export enum PaymentMethod {
  Cash = 'Cash',
  Transfer = 'Transfer',
  Card = 'Card',
}

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
