import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, clinicaSession, errorMessage, tenantFromHost } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiPage, UiButton, UiField],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  slug = tenantFromHost() ?? '';
  email = '';
  password = '';
  readonly error = signal('');
  readonly loading = signal(false);

  async submit() {
    this.error.set('');
    this.loading.set(true);
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      const token = await firstValueFrom(this.auth.tenantLogin({ email: this.email, password: this.password }));
      clinicaSession.set(token.token, token.tenantSlug ?? this.slug, token.role, token.name);
      await this.router.navigateByUrl('/panel');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
