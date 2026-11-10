import { TenantRole } from 'sdk';

// How the first step of an appointment's trail reads, by who created it. No author means the system loaded it.
export const appointmentCreationLabels: Record<TenantRole, string> = {
  [TenantRole.Patient]: 'El paciente sacó el turno',
  [TenantRole.Secretary]: 'Turno dado por la secretaría',
  [TenantRole.TenantAdmin]: 'Turno dado por la administración',
  [TenantRole.Doctor]: 'Turno dado por el profesional',
};

export const systemCreationLabel = 'Turno cargado por el sistema';
