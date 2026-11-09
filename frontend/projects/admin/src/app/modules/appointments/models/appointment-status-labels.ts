import { AppointmentStatus } from 'sdk';

// Same words as the portal, so front desk reads one name per state on every screen.
export const appointmentStatusLabels: Record<AppointmentStatus, string> = {
  [AppointmentStatus.Booked]: 'Reservado',
  [AppointmentStatus.CheckedIn]: 'En sala de espera',
  [AppointmentStatus.InProgress]: 'En atención',
  [AppointmentStatus.Completed]: 'Atendido',
  [AppointmentStatus.Cancelled]: 'Cancelado',
  [AppointmentStatus.NoShow]: 'Ausente',
  [AppointmentStatus.Rescheduled]: 'Reprogramado',
};

export function appointmentStatusTone(status: AppointmentStatus): 'ok' | 'warn' | 'muted' {
  if (status === AppointmentStatus.Cancelled || status === AppointmentStatus.NoShow) return 'warn';
  return status === AppointmentStatus.Completed ? 'ok' : 'muted';
}
