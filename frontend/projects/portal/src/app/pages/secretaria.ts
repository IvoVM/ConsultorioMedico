import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, ComprobanteDto, ListaEsperaDto, TurnoDto, mensajeError } from 'sdk';
import { UiBadge, UiButton, UiCalendar, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiBadge, UiButton, UiCalendar, UiField, UiTable],
  template: `
    <ui-calendar [fecha]="fecha" (fechaChange)="fecha = $event; cargar()">
      <ui-button (click)="cargar()">Actualizar</ui-button>
    </ui-calendar>
    @if (error()) { <p class="mt-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    <section class="mt-10">
      <h2 class="font-serif text-2xl tracking-tight">Agenda del día</h2>
      <div class="mt-4">
        <ui-table>
          <thead><tr><th>Hora</th><th>Paciente</th><th>Profesional</th><th>Estado</th><th></th></tr></thead>
          <tbody>
            @for (turno of turnos(); track turno.id) {
              <tr>
                <td class="whitespace-nowrap tabular-nums">{{ turno.inicio }}</td>
                <td>{{ turno.paciente }}</td>
                <td>{{ turno.profesional }}</td>
                <td><ui-badge>{{ turno.estado }}</ui-badge></td>
                <td>
                  <div class="flex flex-wrap gap-2">
                    @if (turno.estado === 'Reservado') { <ui-button (click)="admitir(turno)">Admitir</ui-button> }
                    @if (turno.estado === 'Reservado' || turno.estado === 'Admitido') {
                      <ui-button variant="ghost" (click)="cancelar(turno)">Cancelar</ui-button>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </ui-table>
      </div>
    </section>
    <section class="mt-12">
      <h2 class="font-serif text-2xl tracking-tight">Reprogramar</h2>
      <form class="mt-4 grid gap-4 md:grid-cols-3" (ngSubmit)="reprogramar()">
        <ui-field label="Id del turno"><input name="turnoId" [(ngModel)]="turnoId" required /></ui-field>
        <ui-field label="Nuevo inicio"><input name="nuevoInicio" type="datetime-local" [(ngModel)]="nuevoInicio" required /></ui-field>
        <div class="flex items-end"><ui-button type="submit">Reprogramar</ui-button></div>
      </form>
    </section>
    <section class="mt-12">
      <h2 class="font-serif text-2xl tracking-tight">Lista de espera</h2>
      <div class="mt-4">
        <ui-table>
          <thead><tr><th>Paciente</th><th>Notas</th><th></th></tr></thead>
          <tbody>
            @for (item of espera(); track item.id) {
              <tr>
                <td>{{ item.paciente }}</td>
                <td>{{ item.notas }}</td>
                <td><ui-button variant="ghost" (click)="elegirEspera(item)">Usar</ui-button></td>
              </tr>
            }
          </tbody>
        </ui-table>
      </div>
      <form class="mt-4 grid gap-4 md:grid-cols-3" (ngSubmit)="asignar()">
        <ui-field label="Espera"><input [value]="esperaId" disabled /></ui-field>
        <ui-field label="Tipo de turno"><input name="tipoTurnoId" [(ngModel)]="tipoTurnoId" required /></ui-field>
        <div class="flex items-end"><ui-button type="submit">Asignar hueco</ui-button></div>
      </form>
    </section>
    <section class="mt-12">
      <h2 class="font-serif text-2xl tracking-tight">Cobros</h2>
      <div class="mt-4">
        <ui-table>
          <thead><tr><th>Paciente</th><th>Total</th><th>Estado</th><th></th></tr></thead>
          <tbody>
            @for (item of comprobantes(); track item.id) {
              <tr>
                <td>{{ item.paciente }}</td>
                <td class="tabular-nums">{{ item.total }}</td>
                <td>{{ item.estado }}</td>
                <td>
                  @if (item.estado === 'Pendiente') {
                    <div class="flex flex-wrap gap-2">
                      <ui-button (click)="pagar(item, 'Efectivo')">Efectivo</ui-button>
                      <ui-button variant="ghost" (click)="pagar(item, 'Transferencia')">Transferencia</ui-button>
                      <ui-button variant="ghost" (click)="pagar(item, 'Tarjeta')">Tarjeta</ui-button>
                    </div>
                  }
                </td>
              </tr>
            }
          </tbody>
        </ui-table>
      </div>
    </section>
  `,
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
