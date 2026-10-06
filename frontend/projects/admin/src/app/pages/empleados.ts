import { Component, inject, signal } from '@angular/core';
import { EmpleadoCreadoDto, FilaRechazadaDto, ClinicaClient, mensajeError } from 'sdk';
import { UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiButton, UiTable, UiEmpty],
  template: `
    <section class="mb-10 max-w-prose">
      <p class="text-sm leading-relaxed text-ink/75">
        CSV con encabezado: nombre,apellido,email,rol,matricula,especialidad. Roles: Medico, Secretario o AdminTenant.
      </p>
      <input class="mt-4" type="file" accept=".csv,text/csv" (change)="leer($event)" />
      <div class="mt-4"><ui-button [disabled]="!csv()" (click)="importar()">Importar</ui-button></div>
    </section>
    @if (error()) { <p class="mb-4 text-sm text-pulse" role="alert">{{ error() }}</p> }
    @if (creados().length) {
      <h2 class="mb-4 font-serif text-2xl tracking-tight">Claves temporales</h2>
      <ui-table>
        <thead><tr><th>Fila</th><th>Email</th><th>Rol</th><th>Clave</th></tr></thead>
        <tbody>
          @for (item of creados(); track item.email) {
            <tr><td>{{ item.fila }}</td><td>{{ item.email }}</td><td>{{ item.rol }}</td><td>{{ item.claveTemporal }}</td></tr>
          }
        </tbody>
      </ui-table>
    }
    @if (rechazados().length) {
      <h2 class="mb-4 mt-10 font-serif text-2xl tracking-tight">Rechazados</h2>
      <ui-table>
        <thead><tr><th>Fila</th><th>Motivo</th></tr></thead>
        <tbody>@for (item of rechazados(); track item.fila) { <tr><td>{{ item.fila }}</td><td>{{ item.motivo }}</td></tr> }</tbody>
      </ui-table>
    }
    @if (!creados().length && !rechazados().length) { <ui-empty message="Todavía no importaste un archivo." /> }
  `,
})
export class EmpleadosPage {
  private readonly api = inject(ClinicaClient);
  readonly csv = signal('');
  readonly creados = signal<EmpleadoCreadoDto[]>([]);
  readonly rechazados = signal<FilaRechazadaDto[]>([]);
  readonly error = signal('');

  leer(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    void file.text().then((text) => this.csv.set(text));
  }

  async importar() {
    this.error.set('');
    try {
      const resultado = await firstValueFrom(this.api.importarEmpleados({ csv: this.csv() }));
      this.creados.set(resultado.creados);
      this.rechazados.set(resultado.rechazados);
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
