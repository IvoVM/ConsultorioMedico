import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, passwordIssue } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField],
  templateUrl: './change-password.html',
})
export class ChangePasswordPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  password = '';
  confirm = '';
  readonly clinicName = signal('Consultorio');
  readonly error = signal('');
  readonly loading = signal(false);

  constructor() {
    void firstValueFrom(this.clinic.clinicProfile())
      .then((profile) => this.clinicName.set(profile.name || 'Consultorio'))
      .catch(() => this.clinicName.set('Consultorio'));
    if (!clinicaSession.mustChangePassword()) void this.router.navigateByUrl('/inicio');
  }

  back() {
    void firstValueFrom(this.auth.logout()).finally(() => {
      clinicaSession.clear();
      location.href = '/ingreso';
    });
  }

  async submit() {
    this.error.set('');
    const issue = passwordIssue(this.password);
    if (issue) {
      this.error.set(issue);
      return;
    }
    if (this.password !== this.confirm) {
      this.error.set('Las contraseñas no coinciden.');
      return;
    }
    this.loading.set(true);
    try {
      const token = await firstValueFrom(this.auth.changePassword({ password: this.password }));
      clinicaSession.set(token.token, token.role, token.name, token.mustChangePassword);
      await this.router.navigateByUrl('/inicio');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
