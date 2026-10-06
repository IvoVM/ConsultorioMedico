import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, TenantDto, mensajeError } from 'sdk';
import { UiBadge, UiButton, UiEmpty, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable, UiBadge, UiEmpty],
  templateUrl: './tenants.html',
})
export class TenantsPage {
  private readonly api = inject(ClinicaClient);
  readonly tenants = signal<TenantDto[]>([]);
  readonly error = signal('');
  slug = '';
  nombre = '';
  tipo = 'Consultorio';

  constructor() {
    void this.cargar();
  }

  async cargar() {
    try {
      this.tenants.set(await firstValueFrom(this.api.listarTenants()));
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async crear() {
    this.error.set('');
    try {
      await firstValueFrom(this.api.crearTenant({ slug: this.slug, nombre: this.nombre, tipo: this.tipo }));
      this.slug = '';
      this.nombre = '';
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }

  async estado(tenant: TenantDto, estado: string) {
    try {
      await firstValueFrom(this.api.cambiarEstadoTenant(tenant.id, { estado }));
      await this.cargar();
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
