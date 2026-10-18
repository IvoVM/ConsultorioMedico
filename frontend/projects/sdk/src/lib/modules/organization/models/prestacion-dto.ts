export interface InsurerPriceDto {
  payer: string;
  amount: number;
  effectiveFrom: string;
}

export interface PrestacionDto {
  id: string;
  name: string;
  durationMinutes: number;
  particular: number | null;
  particularFrom: string | null;
  insurers: InsurerPriceDto[];
}

export interface PrestacionesDto {
  items: PrestacionDto[];
  insurers: string[];
}
