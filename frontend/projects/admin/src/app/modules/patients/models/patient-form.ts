import { MedicalRecordDto, SaveMedicalRecordCommand } from 'sdk';
import { formatFecha } from 'ui';

// The administrative file as the form edits it. Clinical data is not part of it: only professionals write the
// clinical record, from the portal.
export type PatientForm = { [K in keyof SaveMedicalRecordCommand]-?: string };

export function toForm(record: MedicalRecordDto): PatientForm {
  return {
    firstName: record.firstName,
    lastName: record.lastName,
    documentNumber: record.documentNumber,
    birthDate: record.birthDate.startsWith('0001') ? '' : record.birthDate,
    phone: record.phone,
    healthInsurance: record.healthInsurance ?? '',
    memberNumber: record.memberNumber ?? '',
    emergencyContact: record.emergencyContact ?? '',
    emergencyPhone: record.emergencyPhone ?? '',
  };
}

export function age(birthDate: string) {
  const birth = new Date(`${birthDate}T00:00:00`);
  if (Number.isNaN(birth.getTime()) || birth.getFullYear() < 1901) return null;
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
