import { Injectable } from '@angular/core';
import { ApiClient } from '../../core/api-client';
import { queryString } from '../../core/query-string';
import type { ListParams, PageDto } from '../../core/page';
import type { SaveSupplyCommand, SupplyCountCommand, SupplyDto, SupplyEntryCommand, SupplyMovementDto } from './models/supply-dto';

@Injectable({ providedIn: 'root' })
export class SuppliesService extends ApiClient {
  supplies(query?: { low?: boolean }) {
    return this.http.get<SupplyDto[]>(`${this.base}/api/insumos${queryString(query)}`);
  }

  suppliesPage(query: ListParams & { low?: boolean }) {
    return this.http.get<PageDto<SupplyDto>>(`${this.base}/api/insumos${queryString(query)}`);
  }

  createSupply(body: SaveSupplyCommand) {
    return this.http.post<SupplyDto>(`${this.base}/api/insumos`, body);
  }

  updateSupply(id: string, body: SaveSupplyCommand) {
    return this.http.put<SupplyDto>(`${this.base}/api/insumos/${id}`, body);
  }

  setSupplyActive(id: string, active: boolean) {
    return this.http.put<SupplyDto>(`${this.base}/api/insumos/${id}/estado`, { active });
  }

  addEntry(id: string, body: SupplyEntryCommand) {
    return this.http.post<SupplyDto>(`${this.base}/api/insumos/${id}/ingresos`, body);
  }

  count(id: string, body: SupplyCountCommand) {
    return this.http.post<SupplyDto>(`${this.base}/api/insumos/${id}/conteo`, body);
  }

  movements(id: string, query: ListParams) {
    return this.http.get<PageDto<SupplyMovementDto>>(`${this.base}/api/insumos/${id}/movimientos${queryString(query)}`);
  }
}
