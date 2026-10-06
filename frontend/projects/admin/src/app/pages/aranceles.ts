import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ArancelDto, ClinicaClient, TipoTurnoDto, mensajeError } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  template: `
    <form class="mb-8 grid gap-4 border-b border-rule pb-8 sm:grid-cols-2 xl:grid-cols-4" (ngSubmit)="crear()">
      <ui-field label="Tipo de turno">
        <select name="tipoId" [(ngModel)]="tipoId">
          @for (tipo of tipos(); track tipo.id) { <option [value]="tipo.id">{{ tipo.nombre }}</option> }
        </select>
      </ui-field>
      <ui-field label="Monto"><input name="monto" type="number" [(ngModel)]="monto" required /></ui-field>
      <ui-field label="Vigente desde"><input name="desde" type="date" [(ngModel)]="desde" required /></ui-field>
      <div class="flex items-end"><ui-button type="submit">Guardar arancel</ui-button></div>
    </form>
    @if (error()) { <p class="mb-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    <ui-table>
      <thead><tr><th>Tipo</th><th>Monto</th><th>Desde</th></tr></thead>
      <tbody>
        @for (arancel of aranceles(); track arancel.id) {
          <tr><td>{{ arancel.tipoTurno }}</td><td>{{ arancel.monto }}</td><td>{{ arancel.vigenteDesde }}</td></tr>
        }
      </tbody>
    </ui-table>
  `,
})
export class ArancelesPage {
  private readonly api = inject(ClinicaClient);
  readonly tipos = signal<TipoTurnoDto[]>([]);
  readonly aranceles = signal<ArancelDto[]>([]);
  readonly error = signal('');
  tipoId = '';
  monto = 0;
  desde = new Date().toISOString().slice(0, 10);

  constructor() {
    void this.cargar();
  }

  async cargar() {
    try {
      const [tipos, aranceles] = await Promise.all([firstValueFrom(this.api.tiposTurno()), firstValueFrom(this.api.aranceles())]);
      this.tipos.set(tipos);
      this.aranceles.set(aranceles);
      this.tipoId ||= tipos[0]?.id ?? '';
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async crear() {
    try {
      await firstValueFrom(this.api.crearArancel({ tipoTurnoId: this.tipoId, monto: Number(this.monto), vigenteDesde: this.desde }));
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
