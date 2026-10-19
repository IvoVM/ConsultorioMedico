import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';
import { readClinicName } from './clinic-name';

@Component({
  imports: [FormsModule, UiButton, UiField],
  templateUrl: './register.html',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly clinicName = signal('Consultorio');
  documentNumber = '';
  firstName = '';
  lastName = '';
  phone = '';
  readonly fromRequest = signal(false);
  password = '';
  readonly error = signal('');
  readonly loading = signal(false);

  constructor() {
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
    const query = this.route.snapshot.queryParamMap;
    this.documentNumber = query.get('documento') ?? '';
    this.firstName = query.get('nombre') ?? '';
    this.lastName = query.get('apellido') ?? '';
    this.phone = query.get('telefono') ?? '';
    this.fromRequest.set(!!(this.documentNumber || this.firstName || this.phone));
  }

  async submit() {
    this.error.set('');
    this.loading.set(true);
    try {
      const token = await firstValueFrom(
        this.auth.registerPatient({
          password: this.password,
          firstName: this.firstName,
          lastName: this.lastName,
          documentNumber: this.documentNumber,
          phone: this.phone,
        }),
      );
      clinicaSession.set(token.token, token.role, token.name);
      await this.router.navigateByUrl('/mis-turnos');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
