// What changed a supply's stock: a delivery, a visit that used it, or a correction after counting.
export enum SupplyMovementKind {
  Entry = 'Entry',
  Consumption = 'Consumption',
  Adjustment = 'Adjustment',
}
