import { TenantRole } from 'sdk';

export const roleLabels: Record<TenantRole, string> = {
  [TenantRole.TenantAdmin]: 'Administrador',
  [TenantRole.Doctor]: 'Médico',
  [TenantRole.Secretary]: 'Secretario',
  [TenantRole.Patient]: 'Paciente',
};
