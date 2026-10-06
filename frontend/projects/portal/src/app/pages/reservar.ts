import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClinicaClient, EspecialidadDto, HuecoDto, SedeDto, clinicaSession, mensajeError, tenantDesdeHost } from 'sdk';
import { UiButton, UiCalendar, UiEmpty, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiCalendar, UiEmpty],
  template: `
    <div class="grid gap-4 md:grid-cols-3">
        <ui-field label="Consultorio"><input name="slug" [(ngModel)]="slug" (change)="preparar()" /></ui-field>
        <ui-field label="Sede">
          <select name="sedeId" [(ngModel)]="sedeId">
            @for (sede of sedes(); track sede.id) { <option [value]="sede.id">{{ sede.nombre }}</option> }
          </select>
        </ui-field>
        <ui-field label="Especialidad">
          <select name="especialidadId" [(ngModel)]="especialidadId">
            @for (item of especialidades(); track item.id) { <option [value]="item.id">{{ item.nombre }}</option> }
          </select>
        </ui-field>
      </div>
      <div class="mt-6">
        <ui-calendar [fecha]="fecha" (fechaChange)="fecha = $event; buscar()">
          <ui-button (click)="buscar()">Buscar huecos</ui-button>
        </ui-calendar>
      </div>
      @if (error()) { <p class="mt-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
      @if (huecos().length === 0) {
        <div class="mt-6 max-w-xl space-y-4">
          <ui-empty message="No hay turnos libres para esa búsqueda." />
          @if (session.rol() === 'Paciente') {
            <ui-field label="Nota para la lista de espera"><input name="notas" [(ngModel)]="notas" /></ui-field>
            <ui-button (click)="anotar()">Anotarme en lista de espera</ui-button>
          }
        </div>
      } @else {
        <div class="mt-2">
          @for (hueco of huecos(); track hueco.inicio + hueco.profesionalId) {
            <article class="flex flex-col gap-3 border-b border-rule py-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p class="font-serif text-xl leading-tight">{{ hueco.profesional }}</p>
                <p class="mt-1 text-sm text-ink/70">{{ hueco.tipoTurno }}</p>
              </div>
              <div class="flex items-center justify-between gap-4 sm:justify-end">
                <time class="text-sm tabular-nums text-ink/75">{{ hueco.inicio }}</time>
                <ui-button (click)="reservar(hueco)">Reservar</ui-button>
              </div>
            </article>
          }
        </div>
      }
  `,
})
export class ReservarPage {
  private readonly api = inject(ClinicaClient);
  private readonly router = inject(Router);
  readonly session = clinicaSession;
  readonly sedes = signal<SedeDto[]>([]);
  readonly especialidades = signal<EspecialidadDto[]>([]);
  readonly huecos = signal<HuecoDto[]>([]);
  readonly error = signal('');
  slug = tenantDesdeHost() ?? clinicaSession.tenant() ?? '';
  sedeId = '';
  especialidadId = '';
  fecha = new Date().toISOString().slice(0, 10);
  notas = '';

  constructor() {
    if (this.slug) void this.preparar();
  }

  async preparar() {
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      const [sedes, especialidades] = await Promise.all([firstValueFrom(this.api.sedes()), firstValueFrom(this.api.especialidades())]);
      this.sedes.set(sedes);
      this.especialidades.set(especialidades);
      this.sedeId ||= sedes[0]?.id ?? '';
      this.especialidadId ||= especialidades[0]?.id ?? '';
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async buscar() {
    this.error.set('');
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      this.huecos.set(
        await firstValueFrom(
          this.api.disponibilidad({ sedeId: this.sedeId, especialidadId: this.especialidadId, fecha: this.fecha }),
        ),
      );
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async reservar(hueco: HuecoDto) {
    if (clinicaSession.rol() !== 'Paciente') {
      await this.router.navigateByUrl('/login');
      return;
    }
    try {
      await firstValueFrom(
        this.api.reservarTurno({
          profesionalId: hueco.profesionalId,
          sedeId: hueco.sedeId,
          tipoTurnoId: hueco.tipoTurnoId,
          inicio: hueco.inicio,
          pacienteId: null,
          motivoConsulta: null,
        }),
      );
      await this.router.navigateByUrl('/mis-turnos');
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async anotar() {
    try {
      await firstValueFrom(
        this.api.anotarEspera({
          pacienteId: null,
          profesionalId: null,
          sedeId: this.sedeId,
          especialidadId: this.especialidadId,
          notas: this.notas,
        }),
      );
      this.error.set('Quedaste en la lista de espera.');
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
