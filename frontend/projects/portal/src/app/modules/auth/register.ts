import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, clinicaSession, errorMessage, tenantFromHost } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiPage, UiButton, UiField],
  templateUrl: './register.html',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  slug = tenantFromHost() ?? '';
  documentNumber = '';
  firstName = '';
  lastName = '';
  birthDate = '';
  phone = '';
  email = '';
  password = '';
  readonly error = signal('');

  async submit() {
    this.error.set('');
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      const token = await firstValueFrom(
        this.auth.registerPatient({
          email: this.email,
          password: this.password,
          firstName: this.firstName,
          lastName: this.lastName,
          documentNumber: this.documentNumber,
          birthDate: this.birthDate,
          phone: this.phone,
        }),
      );
      clinicaSession.set(token.token, token.tenantSlug ?? this.slug, token.role, token.name);
      await this.router.navigateByUrl('/reservar');
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
