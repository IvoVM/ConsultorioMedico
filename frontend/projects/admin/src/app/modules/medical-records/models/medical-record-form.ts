import { MedicalRecordDto, SaveMedicalRecordCommand } from 'sdk';
import { formatFecha } from 'ui';

export const BLOOD_TYPES = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', '0+', '0-'];

export type MedicalRecordForm = { [K in keyof SaveMedicalRecordCommand]-?: string };

export function toForm(record: MedicalRecordDto): MedicalRecordForm {
  return {
    firstName: record.firstName,
    lastName: record.lastName,
    documentNumber: record.documentNumber,
    birthDate: record.birthDate,
    phone: record.phone,
    bloodType: record.bloodType ?? '',
    allergies: record.allergies ?? '',
    personalHistory: record.personalHistory ?? '',
    familyHistory: record.familyHistory ?? '',
    currentMedication: record.currentMedication ?? '',
    habits: record.habits ?? '',
    healthInsurance: record.healthInsurance ?? '',
    memberNumber: record.memberNumber ?? '',
    emergencyContact: record.emergencyContact ?? '',
    emergencyPhone: record.emergencyPhone ?? '',
    notes: record.notes ?? '',
  };
}

export function age(birthDate: string) {
  const birth = new Date(`${birthDate}T00:00:00`);
  if (Number.isNaN(birth.getTime())) return null;
  const today = new Date();
  let years = today.getFullYear() - birth.getFullYear();
  const months = today.getMonth() - birth.getMonth();
  if (months < 0 || (months === 0 && today.getDate() < birth.getDate())) years--;
  return years;
}

export function shortDate(value?: string | null) {
  const text = formatFecha(value);
  return text || null;
}
