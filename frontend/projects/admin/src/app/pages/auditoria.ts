import { Component, inject, signal } from '@angular/core';
import { AuditoriaDto, ClinicaClient, mensajeError } from 'sdk';
import { UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable],
  template: `
    @if (error()) { <p class="mb-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    <ui-table>
      <thead><tr><th>Fecha</th><th>Acción</th><th>Entidad</th><th>Detalle</th></tr></thead>
      <tbody>
        @for (item of entradas(); track item.id) {
          <tr><td>{{ item.fecha }}</td><td>{{ item.accion }}</td><td>{{ item.entidad }}</td><td>{{ item.detalle }}</td></tr>
        }
      </tbody>
    </ui-table>
  `,
})
export class AuditoriaPage {
  private readonly api = inject(ClinicaClient);
  readonly entradas = signal<AuditoriaDto[]>([]);
  readonly error = signal('');

  constructor() {
    void firstValueFrom(this.api.auditoria())
      .then((lista) => this.entradas.set(lista))
      .catch((error) => this.error.set(mensajeError(error)));
  }
}
