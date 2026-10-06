import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, ComprobanteDto, ListaEsperaDto, TurnoDto, mensajeError } from 'sdk';
import { UiBadge, UiButton, UiCalendar, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiBadge, UiButton, UiCalendar, UiField, UiTable],
  templateUrl: './secretaria.html',
})
export class SecretariaPage {
  private readonly api = inject(ClinicaClient);
  readonly turnos = signal<TurnoDto[]>([]);
  readonly espera = signal<ListaEsperaDto[]>([]);
  readonly comprobantes = signal<ComprobanteDto[]>([]);
  readonly error = signal('');
  fecha = new Date().toISOString().slice(0, 10);
  turnoId = '';
  nuevoInicio = '';
  esperaId = '';
  tipoTurnoId = '';

  constructor() {
    void this.cargar();
  }

  async cargar() {
    this.error.set('');
    try {
      const [turnos, espera, comprobantes] = await Promise.all([
        firstValueFrom(this.api.turnosDelDia({ fecha: this.fecha })),
        firstValueFrom(this.api.listaEspera()),
        firstValueFrom(this.api.comprobantes()),
      ]);
      this.turnos.set(turnos);
      this.espera.set(espera);
      this.comprobantes.set(comprobantes);
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async admitir(turno: TurnoDto) {
    await this.accion(() => this.api.admitirTurno(turno.id));
  }

  async cancelar(turno: TurnoDto) {
    await this.accion(() => this.api.cancelarTurno(turno.id));
  }

  async reprogramar() {
    await this.accion(() => this.api.reprogramarTurno(this.turnoId, { inicio: new Date(this.nuevoInicio).toISOString() }));
  }

  elegirEspera(item: ListaEsperaDto) {
    this.esperaId = item.id;
  }

  async asignar() {
    await this.accion(() => this.api.asignarEspera(this.esperaId, { inicio: new Date(this.nuevoInicio).toISOString(), tipoTurnoId: this.tipoTurnoId }));
  }

  async pagar(item: ComprobanteDto, metodo: string) {
    await this.accion(() => this.api.pagarComprobante(item.id, { metodo }));
  }

  private async accion(llamada: () => Observable<unknown>) {
    this.error.set('');
    try {
      await firstValueFrom(llamada());
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
