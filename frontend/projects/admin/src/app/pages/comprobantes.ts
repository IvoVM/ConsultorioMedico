import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, ComprobanteDto, mensajeError } from 'sdk';
import { UiBadge, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge],
  template: `
    @if (error()) { <p class="mb-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    <ui-table>
      <thead><tr><th>Paciente</th><th>Total</th><th>Estado</th><th>Pago</th></tr></thead>
      <tbody>
        @for (item of comprobantes(); track item.id) {
          <tr>
            <td>{{ item.paciente }}</td>
            <td>{{ item.total }}</td>
            <td><ui-badge [tone]="item.estado === 'Pagado' ? 'ok' : 'warn'">{{ item.estado }}</ui-badge></td>
            <td>{{ item.metodoPago ?? '—' }}</td>
          </tr>
        }
      </tbody>
    </ui-table>
  `,
})
export class ComprobantesPage {
  private readonly api = inject(ClinicaClient);
  readonly comprobantes = signal<ComprobanteDto[]>([]);
  readonly error = signal('');

  constructor() {
    void firstValueFrom(this.api.comprobantes())
      .then((lista) => this.comprobantes.set(lista))
      .catch((error) => this.error.set(mensajeError(error)));
  }
}
