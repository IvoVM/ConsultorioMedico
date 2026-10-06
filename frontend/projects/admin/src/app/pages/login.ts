import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClinicaClient, clinicaSession, mensajeError } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiPage, UiButton, UiField],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly api = inject(ClinicaClient);
  private readonly router = inject(Router);
  readonly modo = signal<'plataforma' | 'consultorio'>('plataforma');
  slug = '';
  email = '';
  password = '';
  readonly error = signal('');

  async entrar() {
    this.error.set('');
    try {
      if (this.modo() === 'consultorio') clinicaSession.setTenant(this.slug.trim().toLowerCase());
      else clinicaSession.setTenant(null);
      const token = await firstValueFrom(
        this.modo() === 'plataforma'
          ? this.api.loginPlataforma({ email: this.email, password: this.password })
          : this.api.loginTenant({ email: this.email, password: this.password }),
      );
      clinicaSession.set(token.token, token.tenantSlug ?? null, token.rol, token.nombre);
      await this.router.navigateByUrl('/inicio');
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
