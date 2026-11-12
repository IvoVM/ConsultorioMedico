import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService, ClinicService, LoginAccount, TenantRole, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiPassword } from 'ui';
import { firstValueFrom } from 'rxjs';
import { readPending } from '../appointments/pending-booking';
import { readClinicName } from './clinic-name';
import { AppRoute } from '../../app.routes.enum';
import { AuthFrom } from './auth-from';

// One sign-in for patients and staff. The API answers 409 only when the password opens both accounts a document holds,
// and then the person picks which one to enter with.
@Component({
  imports: [FormsModule, RouterLink, UiButton, UiField, UiPassword],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  private readonly activated = inject(ActivatedRoute);
  readonly route = AppRoute;
  readonly account = LoginAccount;
  documentNumber = '';
  password = '';
  readonly clinicName = signal('Consultorio');
  readonly fromRequest = signal(false);
  readonly error = signal('');
  readonly loading = signal(false);
  readonly choosing = signal(false);
  readonly chosen = signal<LoginAccount | null>(null);

  constructor() {
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
    const query = this.activated.snapshot.queryParamMap;
    this.documentNumber = query.get('documento') ?? '';
    this.fromRequest.set(query.get('desde') === AuthFrom.Solicitud);
  }

  registerQuery() {
    const params: Record<string, string> = {};
    if (this.fromRequest()) params['desde'] = AuthFrom.Solicitud;
    if (this.documentNumber) params['documento'] = this.documentNumber;
    return params;
  }

  restart() {
    this.choosing.set(false);
    this.chosen.set(null);
    this.error.set('');
    this.password = '';
  }

  async submit(account?: LoginAccount) {
    this.error.set('');
    this.chosen.set(account ?? null);
    this.loading.set(true);
    try {
      const token = await firstValueFrom(this.auth.login({ documentNumber: this.documentNumber, password: this.password, account }));
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
      if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.Conflict) this.choosing.set(true);
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
