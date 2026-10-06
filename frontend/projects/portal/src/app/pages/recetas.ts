import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, RecetaDto, mensajeError } from 'sdk';
import { UiButton, UiEmpty } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiButton, UiEmpty],
  templateUrl: './recetas.html',
})
export class RecetasPage {
  private readonly api = inject(ClinicaClient);
  readonly recetas = signal<RecetaDto[]>([]);
  readonly error = signal('');

  constructor() {
    void firstValueFrom(this.api.misRecetas())
      .then((lista) => this.recetas.set(lista))
      .catch((error) => this.error.set(mensajeError(error)));
  }

  imprimir() {
    window.print();
  }
}
