import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ClinicaClient, HistoriaMedicaDto, mensajeError } from 'sdk';
import { UiButton, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';
import { FichaMedica, GRUPOS_SANGUINEOS, aFicha, edad, fechaCorta } from './historia-medica';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiField],
  templateUrl: './historia-ficha.html',
})
export class HistoriaFichaPage {
  private readonly api = inject(ClinicaClient);
  private readonly pacienteId = inject(ActivatedRoute).snapshot.paramMap.get('pacienteId') ?? '';
  readonly grupos = GRUPOS_SANGUINEOS;
  readonly fechaCorta = fechaCorta;
  readonly secciones = [
    { id: 'identidad', nombre: 'Datos personales' },
    { id: 'cobertura', nombre: 'Cobertura' },
    { id: 'clinica', nombre: 'Datos clínicos' },
    { id: 'antecedentes', nombre: 'Antecedentes' },
    { id: 'emergencia', nombre: 'Contacto de emergencia' },
  ];

  readonly historia = signal<HistoriaMedicaDto | null>(null);
  readonly ficha = signal<FichaMedica | null>(null);
  readonly original = signal('');
  readonly error = signal('');
  readonly guardando = signal(false);
  readonly guardada = signal(false);
  readonly version = signal(0);

  readonly cambios = computed(() => {
    this.version();
    const ficha = this.ficha();
    return !!ficha && JSON.stringify(ficha) !== this.original();
  });
  readonly edad = computed(() => {
    this.version();
    const fecha = this.ficha()?.fechaNacimiento;
    return fecha ? edad(fecha) : null;
  });

  constructor() {
    void firstValueFrom(this.api.historiaMedica(this.pacienteId))
      .then((historia) => this.cargar(historia))
      .catch((error) => this.error.set(mensajeError(error)));
  }

  tocar() {
    this.version.update((v) => v + 1);
    this.guardada.set(false);
  }

  ir(id: string) {
    const seccion = document.getElementById(id);
    if (!seccion) return;
    const quieto = matchMedia('(prefers-reduced-motion: reduce)').matches;
    seccion.scrollIntoView({ behavior: quieto ? 'auto' : 'smooth', block: 'start' });
    seccion.querySelector<HTMLElement>('input:not([disabled]), textarea')?.focus({ preventScroll: true });
  }

  descartar() {
    this.ficha.set(JSON.parse(this.original()));
  }

  async guardar() {
    const ficha = this.ficha();
    if (!ficha) return;
    this.guardando.set(true);
    this.error.set('');
    try {
      this.cargar(await firstValueFrom(this.api.guardarHistoriaMedica(this.pacienteId, ficha)));
      this.guardada.set(true);
      setTimeout(() => this.guardada.set(false), 2600);
    } catch (error) {
      this.error.set(mensajeError(error));
    } finally {
      this.guardando.set(false);
    }
  }

  private cargar(historia: HistoriaMedicaDto) {
    const ficha = aFicha(historia);
    this.historia.set(historia);
    this.ficha.set(ficha);
    this.original.set(JSON.stringify(ficha));
  }
}
