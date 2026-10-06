import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClinicaClient, clinicaSession, mensajeError } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiPage, UiButton, UiField],
  template: `
    <ui-page frame="auth">
      <div uiBrand class="brand-lockup">
        <img class="brand-logo h-20 w-20" src="logo.svg" alt="Logo del consultorio" width="80" height="80" />
        <p class="mt-5 font-serif text-4xl leading-none text-clinic-deep">Clínica</p>
        <h1 class="mt-3 font-serif text-2xl tracking-tight">Ingreso</h1>
        <p class="mt-1 text-sm text-ink/60">Administración</p>
      </div>
      <form class="auth-form max-w-md space-y-5" (ngSubmit)="entrar()">
        <div class="flex flex-wrap gap-2">
          <ui-button type="button" [variant]="modo() === 'plataforma' ? 'primary' : 'ghost'" (click)="modo.set('plataforma')">Plataforma</ui-button>
          <ui-button type="button" [variant]="modo() === 'consultorio' ? 'primary' : 'ghost'" (click)="modo.set('consultorio')">Consultorio</ui-button>
        </div>
        @if (modo() === 'consultorio') {
          <ui-field label="Slug del consultorio"><input name="slug" [(ngModel)]="slug" required /></ui-field>
        }
        <ui-field label="Email"><input name="email" type="email" [(ngModel)]="email" required /></ui-field>
        <ui-field label="Contraseña"><input name="password" type="password" [(ngModel)]="password" required /></ui-field>
        @if (error()) { <p class="text-sm text-pulse" role="alert">{{ error() }}</p> }
        <ui-button class="w-full sm:w-auto" type="submit">Entrar</ui-button>
      </form>
    </ui-page>
  `,
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
