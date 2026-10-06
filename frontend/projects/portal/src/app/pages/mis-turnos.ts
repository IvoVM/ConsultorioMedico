import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, TurnoDto, mensajeError } from 'sdk';
import { UiBadge, UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge, UiButton, UiEmpty],
  template: `
    @if (error()) { <p class="mb-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    @if (turnos().length === 0) {
      <ui-empty message="No tenés turnos." />
    } @else {
      <ui-table>
        <thead><tr><th>Cuándo</th><th>Profesional</th><th>Sede</th><th>Estado</th><th></th></tr></thead>
        <tbody>
          @for (turno of turnos(); track turno.id) {
            <tr>
              <td>{{ turno.inicio }}</td>
              <td>{{ turno.profesional }}</td>
              <td>{{ turno.sede }}</td>
              <td><ui-badge [tone]="turno.estado === 'Cancelado' ? 'warn' : 'ok'">{{ turno.estado }}</ui-badge></td>
              <td>
                @if (turno.estado === 'Reservado') {
                  <ui-button variant="ghost" (click)="cancelar(turno)">Cancelar</ui-button>
                }
              </td>
            </tr>
          }
        </tbody>
      </ui-table>
    }
  `,
})
export class MisTurnosPage {
  private readonly api = inject(ClinicaClient);
  readonly turnos = signal<TurnoDto[]>([]);
  readonly error = signal('');

  constructor() {
    void this.cargar();
  }

  async cargar() {
    try {
      this.turnos.set(await firstValueFrom(this.api.misTurnos()));
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async cancelar(turno: TurnoDto) {
    try {
      await firstValueFrom(this.api.cancelarTurno(turno.id));
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
