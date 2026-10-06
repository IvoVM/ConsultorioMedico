import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, DiagnosticoDto, TurnoDto, mensajeError } from 'sdk';
import { UiButton, UiCalendar, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiCalendar, UiField, UiTable],
  template: `
    <ui-calendar [fecha]="fecha" (fechaChange)="fecha = $event; cargar()">
      <ui-button (click)="cargar()">Ver agenda</ui-button>
    </ui-calendar>
    @if (error()) { <p class="mt-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    <div class="mt-6">
      <ui-table>
        <thead><tr><th>Hora</th><th>Paciente</th><th>Estado</th><th></th></tr></thead>
        <tbody>
          @for (turno of turnos(); track turno.id) {
            <tr>
              <td class="whitespace-nowrap tabular-nums">{{ turno.inicio }}</td>
              <td>{{ turno.paciente }}</td>
              <td>{{ turno.estado }}</td>
              <td><ui-button variant="ghost" (click)="elegir(turno)">Atender</ui-button></td>
            </tr>
          }
        </tbody>
      </ui-table>
    </div>
    @if (turnoId) {
      <form class="mt-10 grid gap-5 border-t border-rule pt-8" (ngSubmit)="guardar()">
        <div>
          <h2 class="font-serif text-2xl tracking-tight">Encuentro</h2>
          <p class="mt-1 text-sm text-ink/60">Turno {{ turnoId }}</p>
        </div>
        <ui-field label="Nota clínica"><textarea name="nota" rows="5" [(ngModel)]="nota"></textarea></ui-field>
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <ui-field label="Tensión"><input name="ta" [(ngModel)]="tension" /></ui-field>
          <ui-field label="FC"><input name="fc" type="number" [(ngModel)]="frecuencia" /></ui-field>
          <ui-field label="Temperatura"><input name="temp" type="number" step="0.1" [(ngModel)]="temperatura" /></ui-field>
          <ui-field label="Peso"><input name="peso" type="number" step="0.1" [(ngModel)]="peso" /></ui-field>
        </div>
        <ui-field label="Diagnósticos">
          <select name="diagnosticos" multiple [(ngModel)]="diagnosticoIds" class="min-h-28">
            @for (item of diagnosticos(); track item.id) { <option [value]="item.id">{{ item.codigo }} {{ item.nombre }}</option> }
          </select>
        </ui-field>
        <div class="flex flex-wrap gap-2">
          <ui-button type="submit">Guardar encuentro</ui-button>
          <ui-button type="button" variant="ghost" (click)="cerrar()">Cerrar y facturar</ui-button>
        </div>
      </form>
      @if (encuentroId) {
        <form class="mt-10 grid gap-4 border-t border-rule pt-8 md:grid-cols-2" (ngSubmit)="recetar()">
          <h2 class="font-serif text-2xl tracking-tight md:col-span-2">Receta</h2>
          <ui-field label="Medicamento"><input name="med" [(ngModel)]="medicamento" required /></ui-field>
          <ui-field label="Dosis"><input name="dosis" [(ngModel)]="dosis" required /></ui-field>
          <ui-field label="Frecuencia"><input name="frecuenciaMed" [(ngModel)]="frecuenciaMed" required /></ui-field>
          <ui-field label="Duración"><input name="duracion" [(ngModel)]="duracion" required /></ui-field>
          <ui-field label="Indicaciones"><input name="indicaciones" [(ngModel)]="indicaciones" /></ui-field>
          <div class="flex items-end"><ui-button type="submit">Emitir receta</ui-button></div>
        </form>
      }
    }
  `,
})
export class MedicoPage {
  private readonly api = inject(ClinicaClient);
  readonly turnos = signal<TurnoDto[]>([]);
  readonly diagnosticos = signal<DiagnosticoDto[]>([]);
  readonly error = signal('');
  fecha = new Date().toISOString().slice(0, 10);
  turnoId = '';
  encuentroId = '';
  nota = '';
  tension = '';
  frecuencia: number | null = null;
  temperatura: number | null = null;
  peso: number | null = null;
  diagnosticoIds: string[] = [];
  medicamento = '';
  dosis = '';
  frecuenciaMed = '';
  duracion = '';
  indicaciones = '';

  constructor() {
    void this.cargar();
  }

  async cargar() {
    try {
      const [turnos, diagnosticos] = await Promise.all([
        firstValueFrom(this.api.turnosDelDia({ fecha: this.fecha })),
        firstValueFrom(this.api.diagnosticos()),
      ]);
      this.turnos.set(turnos);
      this.diagnosticos.set(diagnosticos);
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  elegir(turno: TurnoDto) {
    this.turnoId = turno.id;
    this.encuentroId = '';
  }

  async guardar() {
    this.error.set('');
    try {
      const encuentro = await firstValueFrom(
        this.api.guardarEncuentro({
          turnoId: this.turnoId,
          nota: this.nota,
          tensionArterial: this.tension || null,
          frecuenciaCardiaca: this.frecuencia,
          temperatura: this.temperatura,
          pesoKg: this.peso,
          diagnosticoIds: this.diagnosticoIds,
        }),
      );
      this.encuentroId = encuentro.id;
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async cerrar() {
    if (!this.encuentroId) await this.guardar();
    if (!this.encuentroId) return;
    try {
      await firstValueFrom(this.api.cerrarEncuentro(this.encuentroId));
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async recetar() {
    try {
      await firstValueFrom(
        this.api.crearReceta({
          encuentroId: this.encuentroId,
          indicaciones: this.indicaciones,
          items: [{ medicamento: this.medicamento, dosis: this.dosis, frecuencia: this.frecuenciaMed, duracion: this.duracion }],
        }),
      );
      this.medicamento = '';
      this.error.set('Receta emitida.');
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
