import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, TurnoDto, mensajeError } from 'sdk';
import { UiBadge, UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge, UiButton, UiEmpty],
  templateUrl: './mis-turnos.html',
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
