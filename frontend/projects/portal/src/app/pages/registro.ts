import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClinicaClient, clinicaSession, mensajeError, tenantDesdeHost } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiPage, UiButton, UiField],
  templateUrl: './registro.html',
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
