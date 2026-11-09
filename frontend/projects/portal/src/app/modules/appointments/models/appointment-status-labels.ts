import { AppointmentStatus } from 'sdk';

export const appointmentStatusLabels: Record<AppointmentStatus, string> = {
  Booked: 'Reservado',
  CheckedIn: 'En sala de espera',
  InProgress: 'En atención',
  Completed: 'Atendido',
  Cancelled: 'Cancelado',
  NoShow: 'Ausente',
  Rescheduled: 'Reprogramado',
};
