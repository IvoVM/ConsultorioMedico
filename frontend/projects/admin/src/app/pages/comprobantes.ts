import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, ComprobanteDto, mensajeError } from 'sdk';
import { UiBadge, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge],
  templateUrl: './comprobantes.html',
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
