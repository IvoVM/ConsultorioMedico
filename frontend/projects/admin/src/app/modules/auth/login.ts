import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiPage, UiButton, UiField],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  email = '';
  password = '';
  readonly clinicName = signal('Consultorio');
  readonly error = signal('');
  readonly loading = signal(false);

  constructor() {
    void this.loadClinic();
  }

  private async loadClinic() {
    try {
      const profile = await firstValueFrom(this.clinic.clinicProfile());
      this.clinicName.set(profile.name);
    } catch {
      this.clinicName.set('Consultorio');
    }
  }

  async submit() {
    this.error.set('');
    this.loading.set(true);
    try {
      const token = await firstValueFrom(this.auth.login({ email: this.email, password: this.password }));
      clinicaSession.set(token.token, token.role, token.name);
      await this.router.navigateByUrl('/inicio');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
