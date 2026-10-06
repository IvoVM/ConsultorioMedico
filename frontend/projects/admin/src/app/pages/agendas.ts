import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BloqueAgendaDto, BloqueoDto, ClinicaClient, ProfesionalDto, SedeDto, TipoTurnoDto, mensajeError } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  template: `
    <div class="mb-8 grid max-w-xl gap-4">
      <ui-field label="Profesional">
        <select name="profesionalId" [(ngModel)]="profesionalId" (ngModelChange)="cargarAgenda()">
          @for (pro of profesionales(); track pro.id) { <option [value]="pro.id">{{ pro.apellido }}, {{ pro.nombre }}</option> }
        </select>
      </ui-field>
    </div>
    @if (error()) { <p class="mb-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    <h2 class="font-serif text-2xl tracking-tight">Semana</h2>
    <form class="mb-6 mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-6" (ngSubmit)="agregarBloque()">
      <ui-field label="Día">
        <select name="dia" [(ngModel)]="dia">
          @for (item of dias; track item.valor) { <option [value]="item.valor">{{ item.nombre }}</option> }
        </select>
      </ui-field>
      <ui-field label="Desde"><input name="desde" type="time" [(ngModel)]="desde" required /></ui-field>
      <ui-field label="Hasta"><input name="hasta" type="time" [(ngModel)]="hasta" required /></ui-field>
      <ui-field label="Sede">
        <select name="sedeId" [(ngModel)]="sedeId">
          @for (sede of sedes(); track sede.id) { <option [value]="sede.id">{{ sede.nombre }}</option> }
        </select>
      </ui-field>
      <ui-field label="Tipo">
        <select name="tipoId" [(ngModel)]="tipoId">
          @for (tipo of tipos(); track tipo.id) { <option [value]="tipo.id">{{ tipo.nombre }}</option> }
        </select>
      </ui-field>
      <div class="flex items-end"><ui-button type="submit">Sumar bloque</ui-button></div>
    </form>
    <ui-table>
      <thead><tr><th>Día</th><th>Horario</th><th>Sede</th><th></th></tr></thead>
      <tbody>
        @for (bloque of bloques(); track bloque.id; let i = $index) {
          <tr>
            <td>{{ bloque.dia }}</td>
            <td>{{ bloque.horaDesde }} - {{ bloque.horaHasta }}</td>
            <td>{{ nombreSede(bloque.sedeId) }}</td>
            <td><ui-button variant="ghost" (click)="quitar(i)">Quitar</ui-button></td>
          </tr>
        }
      </tbody>
    </ui-table>
    <div class="my-6"><ui-button (click)="guardar()">Publicar agenda</ui-button></div>
    <h2 class="font-serif text-2xl tracking-tight">Bloqueos</h2>
    <form class="mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-4" (ngSubmit)="bloquear()">
      <ui-field label="Inicio del bloqueo"><input name="inicio" type="datetime-local" [(ngModel)]="inicio" required /></ui-field>
      <ui-field label="Fin"><input name="fin" type="datetime-local" [(ngModel)]="fin" required /></ui-field>
      <ui-field label="Motivo"><input name="motivo" [(ngModel)]="motivo" required /></ui-field>
      <div class="flex items-end"><ui-button type="submit">Bloquear</ui-button></div>
    </form>
    <div class="mt-6">
      <ui-table>
        <thead><tr><th>Desde</th><th>Hasta</th><th>Motivo</th><th></th></tr></thead>
        <tbody>
          @for (bloqueo of bloqueos(); track bloqueo.id) {
            <tr>
              <td>{{ bloqueo.inicio }}</td>
              <td>{{ bloqueo.fin }}</td>
              <td>{{ bloqueo.motivo }}</td>
              <td><ui-button variant="danger" (click)="eliminarBloqueo(bloqueo)">Quitar</ui-button></td>
            </tr>
          }
        </tbody>
      </ui-table>
    </div>
  `,
})
export class AgendasPage {
  private readonly api = inject(ClinicaClient);
  readonly dias = [
    { nombre: 'Lunes', valor: 'Monday' },
    { nombre: 'Martes', valor: 'Tuesday' },
    { nombre: 'Miércoles', valor: 'Wednesday' },
    { nombre: 'Jueves', valor: 'Thursday' },
    { nombre: 'Viernes', valor: 'Friday' },
    { nombre: 'Sábado', valor: 'Saturday' },
  ];
  readonly profesionales = signal<ProfesionalDto[]>([]);
  readonly sedes = signal<SedeDto[]>([]);
  readonly tipos = signal<TipoTurnoDto[]>([]);
  readonly bloques = signal<BloqueAgendaDto[]>([]);
  readonly bloqueos = signal<BloqueoDto[]>([]);
  readonly error = signal('');
  profesionalId = '';
  dia = 'Monday';
  desde = '09:00';
  hasta = '13:00';
  sedeId = '';
  tipoId = '';
  inicio = '';
  fin = '';
  motivo = '';

  constructor() {
    void this.cargarBase();
  }

  async cargarBase() {
    try {
      const [profesionales, sedes, tipos] = await Promise.all([
        firstValueFrom(this.api.profesionales({})),
        firstValueFrom(this.api.sedes()),
        firstValueFrom(this.api.tiposTurno()),
      ]);
      this.profesionales.set(profesionales);
      this.sedes.set(sedes);
      this.tipos.set(tipos);
      this.profesionalId = profesionales[0]?.id ?? '';
      this.sedeId = sedes[0]?.id ?? '';
      this.tipoId = tipos[0]?.id ?? '';
      if (this.profesionalId) await this.cargarAgenda();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async cargarAgenda() {
    if (!this.profesionalId) return;
    const [bloques, bloqueos] = await Promise.all([
      firstValueFrom(this.api.obtenerAgenda({ profesionalId: this.profesionalId })),
      firstValueFrom(this.api.bloqueos({ profesionalId: this.profesionalId })),
    ]);
    this.bloques.set(bloques);
    this.bloqueos.set(bloqueos);
  }

  nombreSede(id: string) {
    return this.sedes().find((sede) => sede.id === id)?.nombre ?? '';
  }

  agregarBloque() {
    this.bloques.update((lista) => [
      ...lista,
      {
        id: crypto.randomUUID(),
        dia: this.dia,
        horaDesde: this.desde.length === 5 ? `${this.desde}:00` : this.desde,
        horaHasta: this.hasta.length === 5 ? `${this.hasta}:00` : this.hasta,
        sedeId: this.sedeId,
        tipoTurnoId: this.tipoId,
      },
    ]);
  }

  quitar(index: number) {
    this.bloques.update((lista) => lista.filter((_, i) => i !== index));
  }

  async guardar() {
    this.error.set('');
    try {
      const bloques = await firstValueFrom(this.api.guardarAgenda({ profesionalId: this.profesionalId, bloques: this.bloques() }));
      this.bloques.set(bloques);
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async bloquear() {
    this.error.set('');
    try {
      await firstValueFrom(
        this.api.crearBloqueo({
          profesionalId: this.profesionalId,
          inicio: new Date(this.inicio).toISOString(),
          fin: new Date(this.fin).toISOString(),
          motivo: this.motivo,
        }),
      );
      this.motivo = '';
      await this.cargarAgenda();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async eliminarBloqueo(bloqueo: BloqueoDto) {
    await firstValueFrom(this.api.eliminarBloqueo(bloqueo.id));
    await this.cargarAgenda();
  }
}
