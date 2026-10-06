import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClinicService, clinicaSession } from 'sdk';
import { firstValueFrom } from 'rxjs';
import { roleLabel } from './models/role-labels';

@Component({
  imports: [RouterLink],
  templateUrl: './dashboard.html',
})
export class DashboardPage {
  private readonly clinic = inject(ClinicService);
  readonly session = clinicaSession;
  readonly roleLabel = roleLabel;
  readonly clinicName = signal('');

  constructor() {
    void this.loadClinic();
  }

  private async loadClinic() {
    try {
      const profile = await firstValueFrom(this.clinic.clinicProfile());
      this.clinicName.set(profile.name);
    } catch {
      this.clinicName.set('');
    }
  }
}
