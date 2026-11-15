import type { SupplyMovementKind } from './supply-movement-kind';

export interface SupplyDto {
  id: string;
  name: string;
  unit: string;
  stock: number;
  minStock: number;
  isActive: boolean;
  // At or below the minimum: time to order more.
  low: boolean;
}

export interface SaveSupplyCommand {
  name: string;
  unit: string;
  minStock: number;
}

// Stock that came in: a delivery or a purchase.
export interface SupplyEntryCommand {
  quantity: number;
  note?: string | null;
}

// What was counted on the shelf; the difference goes in as an adjustment with its reason.
export interface SupplyCountCommand {
  stock: number;
  reason: string;
}

export interface SupplyMovementDto {
  id: string;
  kind: SupplyMovementKind;
  quantity: number;
  at: string;
  note?: string | null;
  user?: string | null;
  appointmentId?: string | null;
  patient?: string | null;
}
