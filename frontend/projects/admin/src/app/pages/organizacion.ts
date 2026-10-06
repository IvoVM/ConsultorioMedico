import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, EspecialidadDto, SedeDto, ServicioDto, TipoTurnoDto, mensajeError } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  template: `
    <div class="mb-8 flex gap-2 overflow-x-auto border-b border-rule pb-4">
      @for (item of secciones; track item) {
        <ui-button type="button" [variant]="seccion() === item ? 'primary' : 'ghost'" (click)="seccion.set(item)">{{ item }}</ui-button>
      }
    </div>
    @if (error()) { <p class="mb-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    @if (seccion() === 'Sedes') {
      <form class="mb-6 grid gap-4 md:grid-cols-3" (ngSubmit)="crearSede()">
        <ui-field label="Nombre"><input name="sedeNombre" [(ngModel)]="sedeNombre" required /></ui-field>
        <ui-field label="Dirección"><input name="direccion" [(ngModel)]="direccion" required /></ui-field>
        <div class="flex items-end"><ui-button type="submit">Agregar sede</ui-button></div>
      </form>
      <ui-table>
        <thead><tr><th>Nombre</th><th>Dirección</th></tr></thead>
        <tbody>@for (sede of sedes(); track sede.id) { <tr><td>{{ sede.nombre }}</td><td>{{ sede.direccion }}</td></tr> }</tbody>
      </ui-table>
    }
    @if (seccion() === 'Servicios') {
      <form class="mb-6 grid gap-4 md:grid-cols-3" (ngSubmit)="crearServicio()">
        <ui-field label="Sede">
          <select name="sedeId" [(ngModel)]="sedeId" required>
            @for (sede of sedes(); track sede.id) { <option [value]="sede.id">{{ sede.nombre }}</option> }
          </select>
        </ui-field>
        <ui-field label="Servicio"><input name="servicioNombre" [(ngModel)]="servicioNombre" required /></ui-field>
        <div class="flex items-end"><ui-button type="submit">Agregar servicio</ui-button></div>
      </form>
      <ui-table>
        <thead><tr><th>Servicio</th><th>Sede</th></tr></thead>
        <tbody>@for (servicio of servicios(); track servicio.id) { <tr><td>{{ servicio.nombre }}</td><td>{{ nombreSede(servicio.sedeId) }}</td></tr> }</tbody>
      </ui-table>
    }
    @if (seccion() === 'Especialidades') {
      <form class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end" (ngSubmit)="crearEspecialidad()">
        <ui-field class="sm:w-80" label="Nombre"><input name="especialidad" [(ngModel)]="especialidad" required /></ui-field>
        <div class="flex items-end"><ui-button type="submit">Agregar</ui-button></div>
      </form>
      <ui-table>
        <thead><tr><th>Especialidad</th></tr></thead>
        <tbody>@for (item of especialidades(); track item.id) { <tr><td>{{ item.nombre }}</td></tr> }</tbody>
      </ui-table>
    }
    @if (seccion() === 'Tipos de turno') {
      <form class="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4" (ngSubmit)="crearTipo()">
        <ui-field label="Nombre"><input name="tipoNombre" [(ngModel)]="tipoNombre" required /></ui-field>
        <ui-field label="Minutos"><input name="duracion" type="number" [(ngModel)]="duracion" required /></ui-field>
        <ui-field label="Especialidad">
          <select name="especialidadId" [(ngModel)]="especialidadId">
            <option value="">Todas</option>
            @for (item of especialidades(); track item.id) { <option [value]="item.id">{{ item.nombre }}</option> }
          </select>
        </ui-field>
        <div class="flex items-end"><ui-button type="submit">Agregar</ui-button></div>
      </form>
      <ui-table>
        <thead><tr><th>Tipo</th><th>Duración</th></tr></thead>
        <tbody>@for (tipo of tipos(); track tipo.id) { <tr><td>{{ tipo.nombre }}</td><td>{{ tipo.duracionMinutos }} min</td></tr> }</tbody>
      </ui-table>
    }
  `,
})
export class OrganizacionPage {
  private readonly api = inject(ClinicaClient);
  readonly secciones = ['Sedes', 'Servicios', 'Especialidades', 'Tipos de turno'] as const;
  readonly seccion = signal<(typeof this.secciones)[number]>('Sedes');
  readonly sedes = signal<SedeDto[]>([]);
  readonly servicios = signal<ServicioDto[]>([]);
  readonly especialidades = signal<EspecialidadDto[]>([]);
  readonly tipos = signal<TipoTurnoDto[]>([]);
  readonly error = signal('');
  sedeNombre = '';
  direccion = '';
  sedeId = '';
  servicioNombre = '';
  especialidad = '';
  tipoNombre = '';
  duracion = 30;
  especialidadId = '';

  constructor() {
    void this.cargar();
  }

  async cargar() {
    try {
      const [sedes, servicios, especialidades, tipos] = await Promise.all([
        firstValueFrom(this.api.sedes()),
        firstValueFrom(this.api.servicios()),
        firstValueFrom(this.api.especialidades()),
        firstValueFrom(this.api.tiposTurno()),
      ]);
      this.sedes.set(sedes);
      this.servicios.set(servicios);
      this.especialidades.set(especialidades);
      this.tipos.set(tipos);
      this.sedeId ||= sedes[0]?.id ?? '';
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  nombreSede(id: string) {
    return this.sedes().find((sede) => sede.id === id)?.nombre ?? '';
  }

  async crearSede() {
    await this.guardar(() => this.api.crearSede({ nombre: this.sedeNombre, direccion: this.direccion }));
    this.sedeNombre = '';
    this.direccion = '';
  }

  async crearServicio() {
    await this.guardar(() => this.api.crearServicio({ sedeId: this.sedeId, nombre: this.servicioNombre }));
    this.servicioNombre = '';
  }

  async crearEspecialidad() {
    await this.guardar(() => this.api.crearEspecialidad({ nombre: this.especialidad }));
    this.especialidad = '';
  }

  async crearTipo() {
    await this.guardar(() =>
      this.api.crearTipoTurno({
        nombre: this.tipoNombre,
        duracionMinutos: Number(this.duracion),
        especialidadId: this.especialidadId || null,
      }),
    );
    this.tipoNombre = '';
  }

  private async guardar(accion: () => Observable<unknown>) {
    this.error.set('');
    try {
      await firstValueFrom(accion());
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
