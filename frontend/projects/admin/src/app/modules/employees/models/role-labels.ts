import { TenantRole } from 'sdk';

export const roleLabels: Record<TenantRole | 'SuperAdmin', string> = {
  SuperAdmin: 'Superadministrador',
  TenantAdmin: 'Administrador',
  Doctor: 'Médico',
  Secretary: 'Secretario',
  Patient: 'Paciente',
};
