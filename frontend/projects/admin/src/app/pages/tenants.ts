import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicaClient, TenantDto, TenantStatus, TenantType, errorMessage, tenantStatusLabels, tenantTypeLabels } from 'sdk';
import { UiBadge, UiButton, UiEmpty, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable, UiBadge, UiEmpty],
  templateUrl: './tenants.html',
})
export class TenantsPage {
  private readonly api = inject(ClinicaClient);
  readonly typeLabels = tenantTypeLabels;
  readonly statusLabels = tenantStatusLabels;
  readonly types = Object.keys(tenantTypeLabels) as TenantType[];
  readonly tenants = signal<TenantDto[]>([]);
  readonly error = signal('');
  slug = '';
  name = '';
  type: TenantType = 'Practice';

  constructor() {
    void this.load();
  }

  async load() {
    try {
      this.tenants.set(await firstValueFrom(this.api.listTenants()));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async create() {
    this.error.set('');
    try {
      await firstValueFrom(this.api.createTenant({ slug: this.slug, name: this.name, type: this.type }));
      this.slug = '';
      this.name = '';
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async changeStatus(tenant: TenantDto, status: TenantStatus) {
    try {
      await firstValueFrom(this.api.changeTenantStatus(tenant.id, { status }));
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
