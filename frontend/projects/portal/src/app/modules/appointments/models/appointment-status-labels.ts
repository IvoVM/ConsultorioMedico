import { AppointmentStatus } from 'sdk';

export const appointmentStatusLabels: Record<AppointmentStatus, string> = {
  Booked: 'Reservado',
  CheckedIn: 'Admitido',
  InProgress: 'En atención',
  Completed: 'Atendido',
  Cancelled: 'Cancelado',
  NoShow: 'Ausente',
};
