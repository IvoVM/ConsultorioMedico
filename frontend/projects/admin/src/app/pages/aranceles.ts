import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ArancelDto, ClinicaClient, TipoTurnoDto, mensajeError } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './aranceles.html',
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
