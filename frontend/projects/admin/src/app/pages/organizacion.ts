import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, EspecialidadDto, SedeDto, ServicioDto, TipoTurnoDto, mensajeError } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './organizacion.html',
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
