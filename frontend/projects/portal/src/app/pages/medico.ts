import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, DiagnosticoDto, TurnoDto, mensajeError } from 'sdk';
import { UiButton, UiCalendar, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiCalendar, UiField, UiTable],
  templateUrl: './medico.html',
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
