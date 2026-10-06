import { AppointmentStatus, DayOfWeek, InvoiceStatus, PaymentMethod, TenantRole, TenantStatus, TenantType, WaitlistStatus } from './clinica-client';

export const tenantTypeLabels: Record<TenantType, string> = {
  Practice: 'Consultorio',
  Hospital: 'Hospital',
};

export const tenantStatusLabels: Record<TenantStatus, string> = {
  Active: 'Activo',
  Suspended: 'Suspendido',
  Deactivated: 'Dado de baja',
};

export const roleLabels: Record<TenantRole | 'SuperAdmin', string> = {
  SuperAdmin: 'Superadministrador',
  TenantAdmin: 'Administrador',
  Doctor: 'Médico',
  Secretary: 'Secretario',
  Patient: 'Paciente',
};

export const appointmentStatusLabels: Record<AppointmentStatus, string> = {
  Booked: 'Reservado',
  CheckedIn: 'Admitido',
  InProgress: 'En atención',
  Completed: 'Atendido',
  Cancelled: 'Cancelado',
  NoShow: 'Ausente',
};

export const waitlistStatusLabels: Record<WaitlistStatus, string> = {
  Pending: 'Pendiente',
  Offered: 'Ofrecido',
  Accepted: 'Aceptado',
  Cancelled: 'Cancelado',
};

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

export const dayLabels: Record<DayOfWeek, string> = {
  Sunday: 'Domingo',
  Monday: 'Lunes',
  Tuesday: 'Martes',
  Wednesday: 'Miércoles',
  Thursday: 'Jueves',
  Friday: 'Viernes',
  Saturday: 'Sábado',
};

export function roleLabel(role: string | null | undefined) {
  return role ? (roleLabels[role as keyof typeof roleLabels] ?? role) : '';
}
