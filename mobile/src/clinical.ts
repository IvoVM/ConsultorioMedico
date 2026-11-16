// The clinical record's kinds of page and background facts, with the same names as the API.
export enum ClinicalEntryKind {
  Visit = 'Visit',
  Amendment = 'Amendment',
  Background = 'Background',
  Consent = 'Consent',
}

export enum BackgroundKind {
  BloodType = 'BloodType',
  Allergies = 'Allergies',
  Medication = 'Medication',
  PersonalHistory = 'PersonalHistory',
  FamilyHistory = 'FamilyHistory',
  Habits = 'Habits',
  Notes = 'Notes',
}

export const backgroundLabels: Record<BackgroundKind, string> = {
  [BackgroundKind.BloodType]: 'Grupo sanguíneo',
  [BackgroundKind.Allergies]: 'Alergias',
  [BackgroundKind.Medication]: 'Medicación habitual',
  [BackgroundKind.PersonalHistory]: 'Antecedentes personales',
  [BackgroundKind.FamilyHistory]: 'Antecedentes familiares',
  [BackgroundKind.Habits]: 'Hábitos',
  [BackgroundKind.Notes]: 'Observaciones',
};

export function entryTitle(entry: { kind: ClinicalEntryKind; backgroundKind?: BackgroundKind | null; amendsFolio?: number | null }) {
  switch (entry.kind) {
    case ClinicalEntryKind.Visit:
      return 'Consulta';
    case ClinicalEntryKind.Amendment:
      return `Corrección del folio ${entry.amendsFolio ?? ''}`.trim();
    case ClinicalEntryKind.Background:
      return entry.backgroundKind ? backgroundLabels[entry.backgroundKind] : 'Antecedente';
    case ClinicalEntryKind.Consent:
      return 'Consentimiento informado';
  }
}
