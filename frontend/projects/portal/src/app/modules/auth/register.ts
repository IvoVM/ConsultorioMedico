import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService, ClinicService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiPassword, formatPhone, passwordIssue } from 'ui';
import { firstValueFrom } from 'rxjs';
import { readClinicName } from './clinic-name';
import { AppRoute } from '../../app.routes.enum';
import { AuthFrom } from './auth-from';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiField, UiPassword],
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
  readonly paths = AppRoute;
  readonly fromRequest = signal(false);
  password = '';
  confirm = '';
  readonly error = signal('');
  readonly loading = signal(false);

  constructor() {
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
    const query = this.route.snapshot.queryParamMap;
    this.documentNumber = query.get('documento') ?? '';
    this.firstName = query.get('nombre') ?? '';
    this.lastName = query.get('apellido') ?? '';
    this.phone = query.get('telefono') ?? '';
    this.fromRequest.set(query.get('desde') === AuthFrom.Solicitud);
  }

  loginQuery() {
    const params: Record<string, string> = {};
    if (this.fromRequest()) params['desde'] = AuthFrom.Solicitud;
    if (this.documentNumber) params['documento'] = this.documentNumber;
    return params;
  }

  normalizePhone() {
    const formatted = formatPhone(this.phone);
    if (formatted) this.phone = formatted;
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
    const phone = formatPhone(this.phone);
    if (!phone) {
      this.error.set('El teléfono no es válido.');
      return;
    }
    this.phone = phone;
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
      await this.router.navigateByUrl(AppRoute.MisTurnos);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
