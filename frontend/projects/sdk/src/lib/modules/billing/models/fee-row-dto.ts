export interface FeeRowDto {
  appointmentTypeId: string;
  service: string;
  amount?: number | null;
  effectiveFrom?: string | null;
}
