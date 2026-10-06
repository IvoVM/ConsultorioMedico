import { TenantRole } from 'sdk';

export const roleLabels: Record<TenantRole | 'SuperAdmin', string> = {
  SuperAdmin: 'Superadministrador',
  TenantAdmin: 'Administrador',
  Doctor: 'Médico',
  Secretary: 'Secretario',
  Patient: 'Paciente',
};

export function roleLabel(role: string | null | undefined) {
  return role ? (roleLabels[role as keyof typeof roleLabels] ?? role) : '';
}
