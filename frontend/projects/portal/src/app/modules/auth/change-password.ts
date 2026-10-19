import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, passwordIssue } from 'ui';
import { firstValueFrom } from 'rxjs';
import { readClinicName } from './clinic-name';

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
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
    if (!clinicaSession.mustChangePassword()) {
      const home = clinicaSession.role() === 'Patient' ? '/mis-turnos' : '/panel';
      void this.router.navigateByUrl(home);
    }
  }

  back() {
    const next = clinicaSession.role() === 'Patient' ? '/ingreso/cuenta' : '/ingreso/empleado';
    void firstValueFrom(this.auth.logout()).finally(() => {
      clinicaSession.clear();
      location.href = next;
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
      await this.router.navigateByUrl(token.role === 'Patient' ? '/mis-turnos' : '/panel');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
