import { TenantStatus, TenantType } from 'sdk';

export const tenantTypeLabels: Record<TenantType, string> = {
  Practice: 'Consultorio',
  Hospital: 'Hospital',
};

export const tenantStatusLabels: Record<TenantStatus, string> = {
  Active: 'Activo',
  Suspended: 'Suspendido',
  Deactivated: 'Dado de baja',
};
