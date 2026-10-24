import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, ClinicService, TenantRole, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiPassword } from 'ui';
import { firstValueFrom } from 'rxjs';
import { readPending } from '../appointments/pending-booking';
import { readClinicName } from './clinic-name';
import { AppRoute } from '../../app.routes.enum';
import { AuthFrom } from './auth-from';

@Component({
  imports: [FormsModule, UiButton, UiField, UiPassword],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly staff = this.route.snapshot.data['audience'] === 'staff';
  documentNumber = '';
  password = '';
  readonly clinicName = signal('Consultorio');
  readonly fromRequest = signal(false);
  readonly error = signal('');
  readonly loading = signal(false);

  constructor() {
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
    const query = this.route.snapshot.queryParamMap;
    this.documentNumber = query.get('documento') ?? '';
    this.fromRequest.set(query.get('desde') === AuthFrom.Solicitud);
  }

  async submit() {
    this.error.set('');
    this.loading.set(true);
    try {
      const token = this.staff
        ? await firstValueFrom(this.auth.loginStaff({ documentNumber: this.documentNumber, password: this.password }))
        : await firstValueFrom(this.auth.loginPatient({ documentNumber: this.documentNumber, password: this.password }));
      clinicaSession.set(token.token, token.role, token.name, token.mustChangePassword);
      if (token.mustChangePassword) {
        await this.router.navigateByUrl(AppRoute.Clave);
        return;
      }
      const next = token.role !== TenantRole.Patient
        ? AppRoute.Panel
        : readPending() && !this.fromRequest()
          ? AppRoute.Reservar
          : AppRoute.MisTurnos;
      await this.router.navigateByUrl(next);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
