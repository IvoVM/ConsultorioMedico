import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';
import { readPending } from '../appointments/pending-booking';
import { readClinicName } from './clinic-name';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiField],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  readonly staff = inject(ActivatedRoute).snapshot.data['audience'] === 'staff';
  email = '';
  password = '';
  readonly clinicName = signal('Consultorio');
  readonly error = signal('');
  readonly loading = signal(false);

  constructor() {
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
  }

  async submit() {
    this.error.set('');
    this.loading.set(true);
    try {
      const token = await firstValueFrom(this.auth.login({ email: this.email, password: this.password }));
      clinicaSession.set(token.token, token.role, token.name);
      const next = token.role === 'Patient' && readPending() ? '/reservar' : '/panel';
      await this.router.navigateByUrl(next);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
