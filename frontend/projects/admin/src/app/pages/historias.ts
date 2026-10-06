import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ClinicaClient, HistoriaMedicaDto, mensajeError } from 'sdk';
import { UiButton, UiEmpty, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { FichaMedica, GRUPOS_SANGUINEOS, aFicha, edad, fechaCorta } from './historia-medica';

type Filtro = 'todas' | 'alergias' | 'incompletas';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiTable],
  templateUrl: './historias.html',
})
export class HistoriasPage {
  private readonly api = inject(ClinicaClient);
  readonly grupos = GRUPOS_SANGUINEOS;
  readonly edad = edad;
  readonly fechaCorta = fechaCorta;
  readonly filtros: { id: Filtro; nombre: string }[] = [
    { id: 'todas', nombre: 'Todas' },
    { id: 'alergias', nombre: 'Con alergias' },
    { id: 'incompletas', nombre: 'Sin completar' },
  ];

  readonly historias = signal<HistoriaMedicaDto[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly busqueda = signal('');
  readonly filtro = signal<Filtro>('todas');
  readonly abierta = signal<string | null>(null);
  readonly guardando = signal(false);
  readonly recienGuardada = signal<string | null>(null);
  borrador: FichaMedica | null = null;

  readonly visibles = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    return this.historias().filter((h) => {
      if (this.filtro() === 'alergias' && !h.alergias) return false;
      if (this.filtro() === 'incompletas' && h.actualizadoEn) return false;
      if (!texto) return true;
      return `${h.nombre} ${h.apellido} ${h.documento} ${h.email}`.toLowerCase().includes(texto);
    });
  });

  readonly conteo = computed(() => ({
    todas: this.historias().length,
    alergias: this.historias().filter((h) => h.alergias).length,
    incompletas: this.historias().filter((h) => !h.actualizadoEn).length,
  }));

  constructor() {
    void firstValueFrom(this.api.historiasMedicas())
      .then((lista) => this.historias.set(lista))
      .catch((error) => this.error.set(mensajeError(error)))
      .finally(() => this.cargando.set(false));
  }

  alternar(historia: HistoriaMedicaDto) {
    if (this.abierta() === historia.pacienteId) {
      this.abierta.set(null);
      this.borrador = null;
      return;
    }
    this.error.set('');
    this.borrador = aFicha(historia);
    this.abierta.set(historia.pacienteId);
  }

  async guardar(pacienteId: string) {
    if (!this.borrador) return;
    this.guardando.set(true);
    this.error.set('');
    try {
      const guardada = await firstValueFrom(this.api.guardarHistoriaMedica(pacienteId, this.borrador));
      this.historias.update((lista) => lista.map((h) => (h.pacienteId === pacienteId ? guardada : h)));
      this.abierta.set(null);
      this.borrador = null;
      this.recienGuardada.set(pacienteId);
      setTimeout(() => this.recienGuardada.update((id) => (id === pacienteId ? null : id)), 2400);
    } catch (error) {
      this.error.set(mensajeError(error));
    } finally {
      this.guardando.set(false);
    }
  }
}
