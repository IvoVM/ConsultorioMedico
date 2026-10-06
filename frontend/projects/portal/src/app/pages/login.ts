import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClinicaClient, clinicaSession, mensajeError, tenantDesdeHost } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiPage, UiButton, UiField],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly api = inject(ClinicaClient);
  private readonly router = inject(Router);
  slug = tenantDesdeHost() ?? '';
  email = '';
  password = '';
  readonly error = signal('');

  async entrar() {
    this.error.set('');
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      const token = await firstValueFrom(this.api.loginTenant({ email: this.email, password: this.password }));
      clinicaSession.set(token.token, token.tenantSlug ?? this.slug, token.rol, token.nombre);
      await this.router.navigateByUrl('/panel');
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
