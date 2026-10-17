import type { MedicalRecordDto } from './medical-record-dto';

export interface MedicalRecordPageDto {
  items: MedicalRecordDto[];
  total: number;
  page: number;
  pageSize: number;
  all: number;
  allergies: number;
  incomplete: number;
}
