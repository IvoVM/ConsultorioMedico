import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClinicaClient, EspecialidadDto, HuecoDto, SedeDto, clinicaSession, mensajeError, tenantDesdeHost } from 'sdk';
import { UiButton, UiCalendar, UiEmpty, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiCalendar, UiEmpty],
  templateUrl: './reservar.html',
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
