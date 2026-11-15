export interface InsurerPriceDto {
  payer: string;
  amount: number;
  effectiveFrom: string;
  copay?: number | null;
}

import type { AppointmentKind } from './appointment-kind';
import type { PracticeSupplyDto } from './practice-dto';

export interface PrestacionDto {
  id: string;
  name: string;
  durationMinutes: number;
  particular: number | null;
  particularFrom: string | null;
  insurers: InsurerPriceDto[];
  kind: AppointmentKind;
  code: string | null;
  equipmentId: string | null;
  equipment: string | null;
  supplies: PracticeSupplyDto[];
}

export interface PrestacionesDto {
  items: PrestacionDto[];
  insurers: string[];
}
