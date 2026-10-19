import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiPassword } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiPassword],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  documentNumber = '';
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
      const token = await firstValueFrom(this.auth.login({ documentNumber: this.documentNumber, password: this.password }));
      clinicaSession.set(token.token, token.role, token.name, token.mustChangePassword);
      await this.router.navigateByUrl(token.mustChangePassword ? '/clave' : '/inicio');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
