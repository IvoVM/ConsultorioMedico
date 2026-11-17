import { HttpErrorResponse } from '@angular/common/http';
import { BackgroundKind, ClinicalAccessAction, ClinicalEntryDto, ClinicalEntryKind, ConsentDecision, TenantRole, errorMessage } from 'sdk';
import { Observable, firstValueFrom } from 'rxjs';

export const backgroundLabels: Record<BackgroundKind, string> = {
  [BackgroundKind.BloodType]: 'Grupo sanguíneo',
  [BackgroundKind.Allergies]: 'Alergias',
  [BackgroundKind.Medication]: 'Medicación habitual',
  [BackgroundKind.PersonalHistory]: 'Antecedentes personales',
  [BackgroundKind.FamilyHistory]: 'Antecedentes familiares',
  [BackgroundKind.Habits]: 'Hábitos',
  [BackgroundKind.Notes]: 'Observaciones',
};

export const backgroundKinds = Object.values(BackgroundKind);

export const bloodTypes = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', '0+', '0-'];

export const consentLabels: Record<ConsentDecision, string> = {
  [ConsentDecision.Accepted]: 'Consentimiento informado',
  [ConsentDecision.Revoked]: 'Revocación del consentimiento',
};

export const accessLabels: Record<ClinicalAccessAction, string> = {
  [ClinicalAccessAction.View]: 'Abrió la historia',
  [ClinicalAccessAction.Copy]: 'Descargó una copia',
  [ClinicalAccessAction.Sign]: 'Firmó un folio',
  [ClinicalAccessAction.Amend]: 'Firmó una corrección',
  [ClinicalAccessAction.Attachment]: 'Abrió un adjunto',
  [ClinicalAccessAction.Emergency]: 'La abrió por una urgencia, sin atenderte',
};

export const roleLabels: Record<TenantRole, string> = {
  [TenantRole.TenantAdmin]: 'Administración',
  [TenantRole.Doctor]: 'Profesional',
  [TenantRole.Secretary]: 'Secretaría',
  [TenantRole.Patient]: 'Paciente',
};

// The consent scan the professional attaches: what the server accepts.
export const consentFileTypes = ['application/pdf', 'image/jpeg', 'image/png'];
export const consentMaxBytes = 5 * 1024 * 1024;

export function entryTitle(entry: ClinicalEntryDto) {
  switch (entry.kind) {
    case ClinicalEntryKind.Visit:
      return 'Consulta';
    case ClinicalEntryKind.Amendment:
      return `Corrección del folio ${entry.amendsFolio ?? ''}`.trim();
    case ClinicalEntryKind.Background:
      return entry.backgroundKind ? backgroundLabels[entry.backgroundKind] : 'Antecedente';
    case ClinicalEntryKind.Consent:
      return entry.consentDecision ? consentLabels[entry.consentDecision] : 'Consentimiento';
  }
}

export function vitals(entry: { bloodPressure?: string | null; heartRate?: number | null; temperature?: number | null; weightKg?: number | null }) {
  const parts: string[] = [];
  if (entry.bloodPressure) parts.push(`TA ${entry.bloodPressure}`);
  if (entry.heartRate != null) parts.push(`FC ${entry.heartRate}`);
  if (entry.temperature != null) parts.push(`T ${entry.temperature.toLocaleString('es-AR')} °C`);
  if (entry.weightKg != null) parts.push(`Peso ${entry.weightKg.toLocaleString('es-AR')} kg`);
  return parts.join(' · ');
}

// Downloads a file the API sends back; its errors come as JSON inside a blob.
export async function downloadFile(request: Observable<Blob>, name: string) {
  let blob: Blob;
  try {
    blob = await firstValueFrom(request);
  } catch (error) {
    const body = error instanceof HttpErrorResponse && error.error instanceof Blob ? await error.error.text() : '';
    throw new Error(body ? errorMessage({ error: JSON.parse(body) }) : errorMessage(error));
  }
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = name;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function readBase64(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result).split(',')[1] ?? '');
    reader.onerror = () => reject(new Error('No se pudo leer el archivo.'));
    reader.readAsDataURL(file);
  });
}
