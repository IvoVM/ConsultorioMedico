import type { TenantRole } from '../../employees/models/tenant-role';

export interface AccountUserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: TenantRole;
  documentNumber?: string | null;
}
