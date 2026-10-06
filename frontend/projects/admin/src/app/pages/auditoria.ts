import { Component, inject, signal } from '@angular/core';
import { AuditoriaDto, ClinicaClient, mensajeError } from 'sdk';
import { UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable],
  templateUrl: './auditoria.html',
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
