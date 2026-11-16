import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ClinicalService, SignatureStampDto } from 'sdk';
import { UiButton, UiField, UiModal, UiPassword } from 'ui';
import { firstValueFrom } from 'rxjs';
import { AppRoute } from '../../app.routes.enum';
import { Stamp } from './stamp';

// Every page of the clinical record is signed: the professional types their password again, and what they
// sign can no longer be edited, only corrected with a new signed page. The modal shows the stamp the page
// will carry, with their handwritten signature if they added one.
@Component({
  selector: 'app-sign-modal',
  imports: [FormsModule, RouterLink, UiButton, UiField, UiModal, UiPassword, Stamp],
  templateUrl: './sign-modal.html',
})
export class SignModal {
  private readonly clinical = inject(ClinicalService);
  readonly title = input.required<string>();
  readonly action = input('Firmar');
  readonly busy = input(false);
  readonly error = input('');
  readonly signed = output<string>();
  readonly closed = output<void>();
  readonly stamp = signal<SignatureStampDto | null>(null);
  readonly image = signal<string | null>(null);
  readonly signatureRoute = AppRoute.FirmaSello;
  password = '';

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      const url = this.image();
      if (url) URL.revokeObjectURL(url);
    });
    void this.loadStamp();
  }

  submit() {
    if (!this.password) return;
    this.signed.emit(this.password);
  }

  private async loadStamp() {
    try {
      const stamp = await firstValueFrom(this.clinical.mySignature());
      this.stamp.set(stamp);
      if (stamp.hasImage) this.image.set(URL.createObjectURL(await firstValueFrom(this.clinical.mySignatureImage())));
    } catch {
      // The preview is a courtesy: signing still works and the server says what is missing.
    }
  }
}
