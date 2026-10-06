import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClinicaClient, clinicaSession, mensajeError, tenantDesdeHost } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiPage, UiButton, UiField],
  template: `
    <ui-page frame="auth">
      <div uiBrand class="brand-lockup">
        <img class="brand-logo h-20 w-20" src="logo.svg" alt="Logo del consultorio" width="80" height="80" />
        <p class="mt-5 font-serif text-4xl leading-none text-clinic-deep">Clínica</p>
        <h1 class="mt-3 font-serif text-2xl tracking-tight">Ingreso</h1>
        <p class="mt-1 text-sm text-ink/60">Portal</p>
      </div>
      <form class="auth-form max-w-md space-y-5" (ngSubmit)="entrar()">
        <ui-field label="Consultorio"><input name="slug" [(ngModel)]="slug" required /></ui-field>
        <ui-field label="Email"><input name="email" type="email" [(ngModel)]="email" required /></ui-field>
        <ui-field label="Contraseña"><input name="password" type="password" [(ngModel)]="password" required /></ui-field>
        @if (error()) { <p class="text-sm text-pulse" role="alert">{{ error() }}</p> }
        <div class="flex flex-col gap-4 pt-1 sm:flex-row sm:items-center">
          <ui-button class="w-full sm:w-auto" type="submit">Entrar</ui-button>
          <a routerLink="/registro" class="text-link">Crear cuenta de paciente</a>
        </div>
      </form>
    </ui-page>
  `,
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
