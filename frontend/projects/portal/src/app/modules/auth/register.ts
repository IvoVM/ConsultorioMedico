import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiPage, UiButton, UiField],
  templateUrl: './register.html',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  documentNumber = '';
  firstName = '';
  lastName = '';
  birthDate = '';
  phone = '';
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
      const token = await firstValueFrom(
        this.auth.registerPatient({
          email: this.email,
          password: this.password,
          firstName: this.firstName,
          lastName: this.lastName,
          documentNumber: this.documentNumber,
          birthDate: this.birthDate,
          phone: this.phone,
        }),
      );
      clinicaSession.set(token.token, token.role, token.name);
      await this.router.navigateByUrl('/reservar');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
