import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService, ClinicService, ConsentKind, PrivacyCenterDto, PrivacyNoticeDto, clinicaSession, errorMessage } from 'sdk';
import { FechaPipe, ToastService, UiButton, UiConsent, UiField, UiModal, UiPassword, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';
import { AppRoute } from '../../app.routes.enum';
import { downloadFile } from '../clinical/models/clinical-labels';
import { consentChannelLabels, consentKindLabels } from './models/privacy-labels';

// The patient's rights over their data: what they agreed to, a copy of everything and closing the account.
@Component({
  imports: [FormsModule, RouterLink, FechaPipe, UiButton, UiConsent, UiField, UiModal, UiPassword, UiSkeleton],
  templateUrl: './privacy.html',
})
export class PrivacyPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly toasts = inject(ToastService);
  readonly route = AppRoute;
  readonly consentKind = ConsentKind;
  readonly kindLabels = consentKindLabels;
  readonly channelLabels = consentChannelLabels;
  readonly center = signal<PrivacyCenterDto | null>(null);
  readonly notice = signal<PrivacyNoticeDto | null>(null);
  readonly accepted = signal(false);
  readonly error = signal('');
  readonly busy = signal(false);
  readonly closing = signal(false);
  password = '';

  constructor() {
    void this.load();
    this.clinic.privacyNotice().subscribe({ next: (notice) => this.notice.set(notice) });
  }

  async accept() {
    const notice = this.notice();
    if (!notice || !this.accepted()) {
      this.error.set('Leé el aviso de privacidad y marcá que lo aceptás.');
      return;
    }
    await this.run(async () => {
      this.center.set(await firstValueFrom(this.auth.acceptPrivacy({ privacyVersion: notice.version })));
      this.toasts.success('Aviso aceptado', 'Quedó registrado con la fecha de hoy.');
    });
  }

  async download() {
    await this.run(() => downloadFile(this.auth.myData(), 'mis-datos.json'));
  }

  async closeAccount() {
    if (!this.password) {
      this.error.set('Escribí tu contraseña para confirmar.');
      return;
    }
    await this.run(async () => {
      await firstValueFrom(this.auth.closeAccount({ password: this.password }));
      clinicaSession.clear();
      location.href = AppRoute.Ingreso;
    });
  }

  openClose() {
    this.error.set('');
    this.password = '';
    this.closing.set(true);
  }

  private async load() {
    try {
      this.center.set(await firstValueFrom(this.auth.privacyCenter()));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  private async run(work: () => Promise<void>) {
    this.error.set('');
    this.busy.set(true);
    try {
      await work();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }
}
