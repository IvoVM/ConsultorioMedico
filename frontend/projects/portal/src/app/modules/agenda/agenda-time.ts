import { AgendaProfessionalDto, AgendaRangeDto, AppointmentDto, AppointmentStatus } from 'sdk';

export const SNAP_MINUTES = 5;
export const ROW_MINUTES = 30;
export const PX_PER_MINUTE = 1.7;
export const DEFAULT_DAY_START = 8 * 60;
export const DEFAULT_DAY_END = 20 * 60;

export enum AgendaTone {
  Booked = 'booked',
  Moved = 'moved',
  Waiting = 'waiting',
  Consult = 'consult',
  Done = 'done',
  Missed = 'missed',
}

export const statusTone: Record<AppointmentStatus, AgendaTone> = {
  [AppointmentStatus.Booked]: AgendaTone.Booked,
  [AppointmentStatus.Rescheduled]: AgendaTone.Moved,
  [AppointmentStatus.CheckedIn]: AgendaTone.Waiting,
  [AppointmentStatus.InProgress]: AgendaTone.Consult,
  [AppointmentStatus.Completed]: AgendaTone.Done,
  [AppointmentStatus.NoShow]: AgendaTone.Missed,
  [AppointmentStatus.Cancelled]: AgendaTone.Missed,
};

export function movable(status: AppointmentStatus) {
  return status === AppointmentStatus.Booked || status === AppointmentStatus.Rescheduled || status === AppointmentStatus.CheckedIn;
}

export function minutesOf(iso: string) {
  const date = new Date(iso);
  return date.getHours() * 60 + date.getMinutes();
}

export function clock(minutes: number) {
  const safe = Math.max(0, Math.min(24 * 60, Math.round(minutes)));
  return `${String(Math.floor(safe / 60)).padStart(2, '0')}:${String(safe % 60).padStart(2, '0')}`;
}

export function isoAt(day: string, minutes: number) {
  const [year, month, date] = day.split('-').map(Number);
  const moment = new Date(year, month - 1, date, Math.floor(minutes / 60), minutes % 60, 0, 0);
  const offset = -moment.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  const abs = Math.abs(offset);
  return `${day}T${clock(minutes)}:00${sign}${String(Math.floor(abs / 60)).padStart(2, '0')}:${String(abs % 60).padStart(2, '0')}`;
}

export function shiftDay(day: string, delta: number) {
  const [year, month, date] = day.split('-').map(Number);
  const next = new Date(year, month - 1, date + delta);
  return `${next.getFullYear()}-${String(next.getMonth() + 1).padStart(2, '0')}-${String(next.getDate()).padStart(2, '0')}`;
}

export function dayTitle(day: string) {
  const [year, month, date] = day.split('-').map(Number);
  const label = new Date(year, month - 1, date).toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
  return label.charAt(0).toUpperCase() + label.slice(1);
}

export function spans(ranges: AgendaRangeDto[]) {
  return ranges.map((range) => ({ from: minutesOf(range.start), to: minutesOf(range.end) || 24 * 60 }));
}

export function isOverbook(professional: AgendaProfessionalDto, from: number, to: number) {
  return !spans(professional.ranges).some((range) => range.from <= from && to <= range.to);
}

export function inPast(day: string, minutes: number) {
  return new Date(isoAt(day, minutes)).getTime() <= Date.now();
}

export function durationOf(appointment: AppointmentDto) {
  return Math.max(SNAP_MINUTES, Math.round((new Date(appointment.end).getTime() - new Date(appointment.start).getTime()) / 60000));
}

export function bounds(professionals: AgendaProfessionalDto[], appointments: AppointmentDto[]) {
  let start = DEFAULT_DAY_START;
  let end = DEFAULT_DAY_END;
  for (const professional of professionals) {
    for (const range of spans(professional.ranges)) {
      start = Math.min(start, range.from);
      end = Math.max(end, range.to);
    }
  }
  for (const appointment of appointments) {
    start = Math.min(start, minutesOf(appointment.start));
    end = Math.max(end, minutesOf(appointment.start) + durationOf(appointment));
  }
  return { start: Math.floor(start / 60) * 60, end: Math.min(24 * 60, Math.ceil(end / 60) * 60) };
}
