import { SlotDto } from 'sdk';

const key = 'clinica.turno-pendiente';

export interface PendingBooking {
  professionalId: string;
  professional: string;
  locationId: string;
  appointmentTypeId: string;
  start: string;
}

export function savePending(slot: SlotDto) {
  const pending: PendingBooking = {
    professionalId: slot.professionalId,
    professional: slot.professional,
    locationId: slot.locationId,
    appointmentTypeId: slot.appointmentTypeId,
    start: slot.start,
  };
  sessionStorage.setItem(key, JSON.stringify(pending));
}

export function readPending(): PendingBooking | null {
  const raw = sessionStorage.getItem(key);
  if (!raw) return null;
  try {
    const pending = JSON.parse(raw) as PendingBooking;
    if (!pending.professionalId || !pending.start) return null;
    return pending;
  } catch {
    return null;
  }
}

export function clearPending() {
  sessionStorage.removeItem(key);
}
