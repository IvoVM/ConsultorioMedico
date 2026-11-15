import { SupplyMovementKind } from 'sdk';

export const supplyMovementLabels: Record<SupplyMovementKind, string> = {
  [SupplyMovementKind.Entry]: 'Ingreso',
  [SupplyMovementKind.Consumption]: 'Consumo',
  [SupplyMovementKind.Adjustment]: 'Ajuste',
};
