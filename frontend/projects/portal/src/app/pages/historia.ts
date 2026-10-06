import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, HistoriaDto, clinicaSession, mensajeError } from 'sdk';
import { UiButton, UiEmpty, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiEmpty],
  templateUrl: './historia.html',
})
export class HistoriaPage {
  private readonly api = inject(ClinicaClient);
  readonly session = clinicaSession;
  readonly historia = signal<HistoriaDto | null>(null);
  readonly error = signal('');
  pacienteId = '';

  constructor() {
    if (clinicaSession.rol() === 'Paciente') void this.cargar();
  }

  async cargar() {
    this.error.set('');
    try {
      this.historia.set(await firstValueFrom(this.api.historia(clinicaSession.rol() === 'Paciente' ? {} : { pacienteId: this.pacienteId })));
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
