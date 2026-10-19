import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField],
  templateUrl: './change-password.html',
})
export class ChangePasswordPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  password = '';
  confirm = '';
  readonly error = signal('');
  readonly loading = signal(false);

  constructor() {
    if (!clinicaSession.mustChangePassword()) {
      const home = clinicaSession.role() === 'Patient' ? '/mis-turnos' : '/panel';
      void this.router.navigateByUrl(home);
    }
  }

  async submit() {
    this.error.set('');
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
