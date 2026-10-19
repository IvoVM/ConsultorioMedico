import { Injectable } from '@angular/core';
import { ApiClient } from '../../core/api-client';
import { queryString } from '../../core/query-string';
import type { ListParams, PageDto } from '../../core/page';
import type { AccountUserDto } from './models/account-user-dto';

@Injectable({ providedIn: 'root' })
export class UsersService extends ApiClient {
  usersPage(query: ListParams) {
    return this.http.get<PageDto<AccountUserDto>>(`${this.base}/api/usuarios${queryString(query)}`);
  }

  resetPassword(id: string) {
    return this.http.post<void>(`${this.base}/api/usuarios/${id}/reiniciar-clave`, {});
  }
}
