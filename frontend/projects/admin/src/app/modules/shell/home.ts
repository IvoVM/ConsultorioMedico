import { Component, inject, signal } from '@angular/core';
import { ClinicService } from 'sdk';
import { firstValueFrom } from 'rxjs';

@Component({
  templateUrl: './home.html',
})
export class HomePage {
  private readonly clinic = inject(ClinicService);
  readonly clinicName = signal('este consultorio');

  constructor() {
    void this.loadClinic();
  }

  private async loadClinic() {
    try {
      const profile = await firstValueFrom(this.clinic.clinicProfile());
      this.clinicName.set(profile.name);
    } catch {
      this.clinicName.set('este consultorio');
    }
  }
}
