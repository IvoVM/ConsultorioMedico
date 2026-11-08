import { AppointmentStatus } from 'sdk';
import { UiTone } from 'ui';

// One color per status, shared by the patient flow board and the agenda so both read the same.
export const appointmentStatusTones: Record<AppointmentStatus, UiTone> = {
  [AppointmentStatus.Booked]: UiTone.Muted,
  [AppointmentStatus.Rescheduled]: UiTone.Muted,
  [AppointmentStatus.CheckedIn]: UiTone.Warn,
  [AppointmentStatus.InProgress]: UiTone.Sky,
  [AppointmentStatus.Completed]: UiTone.Ok,
  [AppointmentStatus.NoShow]: UiTone.Bad,
  [AppointmentStatus.Cancelled]: UiTone.Bad,
};
