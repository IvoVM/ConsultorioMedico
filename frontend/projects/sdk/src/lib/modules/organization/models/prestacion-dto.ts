export interface InsurerPriceDto {
  payer: string;
  amount: number;
  effectiveFrom: string;
}

import type { AppointmentKind } from './appointment-kind';

export interface PrestacionDto {
  id: string;
  name: string;
  durationMinutes: number;
  particular: number | null;
  particularFrom: string | null;
  insurers: InsurerPriceDto[];
  kind: AppointmentKind;
}

export interface PrestacionesDto {
  items: PrestacionDto[];
  insurers: string[];
}
