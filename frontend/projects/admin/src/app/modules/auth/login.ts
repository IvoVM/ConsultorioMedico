import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiPage } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiPage, UiButton, UiField],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly mode = signal<'platform' | 'tenant'>('platform');
  slug = '';
  email = '';
  password = '';
  readonly error = signal('');

  async submit() {
    this.error.set('');
    try {
      if (this.mode() === 'tenant') clinicaSession.setTenant(this.slug.trim().toLowerCase());
      else clinicaSession.setTenant(null);
      const credentials = { email: this.email, password: this.password };
      const token = await firstValueFrom(
        this.mode() === 'platform' ? this.auth.platformLogin(credentials) : this.auth.tenantLogin(credentials),
      );
      clinicaSession.set(token.token, token.tenantSlug ?? null, token.role, token.name);
      await this.router.navigateByUrl('/inicio');
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
