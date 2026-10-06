import { Component, inject, signal } from '@angular/core';
import { EmpleadoCreadoDto, FilaRechazadaDto, ClinicaClient, mensajeError } from 'sdk';
import { UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiButton, UiTable, UiEmpty],
  templateUrl: './empleados.html',
})
export class EmpleadosPage {
  private readonly api = inject(ClinicaClient);
  readonly csv = signal('');
  readonly creados = signal<EmpleadoCreadoDto[]>([]);
  readonly rechazados = signal<FilaRechazadaDto[]>([]);
  readonly error = signal('');

  leer(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    void file.text().then((text) => this.csv.set(text));
  }

  async importar() {
    this.error.set('');
    try {
      const resultado = await firstValueFrom(this.api.importarEmpleados({ csv: this.csv() }));
      this.creados.set(resultado.creados);
      this.rechazados.set(resultado.rechazados);
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
