import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService, PatientProfileDto, clinicaSession, errorMessage } from 'sdk';
import { UiButton, UiField, UiSkeleton, formatPhone } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiSkeleton],
  templateUrl: './profile.html',
})
export class ProfilePage {
  private readonly auth = inject(AuthService);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly saved = signal(false);
  firstName = '';
  lastName = '';
  documentNumber = '';
  phone = '';

  constructor() {
    void this.load();
  }

  normalizePhone() {
    const formatted = formatPhone(this.phone);
    if (formatted) this.phone = formatted;
  }

  async save() {
    this.saving.set(true);
    this.error.set('');
    this.saved.set(false);
    this.normalizePhone();
    try {
      const profile = await firstValueFrom(this.auth.updateProfile({
        firstName: this.firstName.trim(),
        lastName: this.lastName.trim(),
        phone: this.phone,
      }));
      this.apply(profile);
      clinicaSession.name.set(`${profile.firstName} ${profile.lastName}`.trim());
      this.saved.set(true);
    } catch (reason) {
      this.error.set(errorMessage(reason));
    } finally {
      this.saving.set(false);
    }
  }

  private async load() {
    try {
      this.apply(await firstValueFrom(this.auth.profile()));
    } catch (reason) {
      this.error.set(errorMessage(reason));
    } finally {
      this.loading.set(false);
    }
  }

  private apply(profile: PatientProfileDto) {
    this.firstName = profile.firstName;
    this.lastName = profile.lastName;
    this.documentNumber = profile.documentNumber;
    this.phone = formatPhone(profile.phone) ?? profile.phone;
  }
}
