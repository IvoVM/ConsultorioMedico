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
        <h1 class="mt-3 font-serif text-2xl tracking-tight">Registro de paciente</h1>
        <p class="mt-1 text-sm text-ink/60">Portal</p>
      </div>
      <form class="auth-form grid max-w-2xl gap-x-4 gap-y-5 sm:grid-cols-2" (ngSubmit)="registrar()">
        <ui-field label="Consultorio"><input name="slug" [(ngModel)]="slug" required /></ui-field>
        <ui-field label="Documento"><input name="documento" [(ngModel)]="documento" required /></ui-field>
        <ui-field label="Nombre"><input name="nombre" [(ngModel)]="nombre" required /></ui-field>
        <ui-field label="Apellido"><input name="apellido" [(ngModel)]="apellido" required /></ui-field>
        <ui-field label="Nacimiento"><input name="nacimiento" type="date" [(ngModel)]="nacimiento" required /></ui-field>
        <ui-field label="Teléfono"><input name="telefono" [(ngModel)]="telefono" required /></ui-field>
        <ui-field label="Email"><input name="email" type="email" [(ngModel)]="email" required /></ui-field>
        <ui-field label="Contraseña"><input name="password" type="password" [(ngModel)]="password" required /></ui-field>
        @if (error()) { <p class="text-sm text-pulse sm:col-span-2" role="alert">{{ error() }}</p> }
        <div class="flex flex-col gap-4 pt-1 sm:col-span-2 sm:flex-row sm:items-center">
          <ui-button class="w-full sm:w-auto" type="submit">Crear cuenta</ui-button>
          <a routerLink="/login" class="text-link">Ya tengo cuenta</a>
        </div>
      </form>
    </ui-page>
  `,
})
export class RegistroPage {
  private readonly api = inject(ClinicaClient);
  private readonly router = inject(Router);
  slug = tenantDesdeHost() ?? '';
  documento = '';
  nombre = '';
  apellido = '';
  nacimiento = '';
  telefono = '';
  email = '';
  password = '';
  readonly error = signal('');

  async registrar() {
    this.error.set('');
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      const token = await firstValueFrom(
        this.api.registrarPaciente({
          email: this.email,
          password: this.password,
          nombre: this.nombre,
          apellido: this.apellido,
          documento: this.documento,
          fechaNacimiento: this.nacimiento,
          telefono: this.telefono,
        }),
      );
      clinicaSession.set(token.token, token.tenantSlug ?? this.slug, token.rol, token.nombre);
      await this.router.navigateByUrl('/reservar');
    } catch (error) {
      this.error.set(mensajeError(error));
    }
  }
}
