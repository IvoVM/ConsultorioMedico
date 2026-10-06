import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BloqueAgendaDto, BloqueoDto, ClinicaClient, ProfesionalDto, SedeDto, TipoTurnoDto, mensajeError } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './agendas.html',
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
