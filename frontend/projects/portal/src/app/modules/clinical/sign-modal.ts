import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UiButton, UiField, UiModal, UiPassword } from 'ui';

// Every page of the clinical record is signed: the professional types their password again, and what they
// sign can no longer be edited, only corrected with a new signed page.
@Component({
  selector: 'app-sign-modal',
  imports: [FormsModule, UiButton, UiField, UiModal, UiPassword],
  templateUrl: './sign-modal.html',
})
export class SignModal {
  readonly title = input.required<string>();
  readonly action = input('Firmar');
  readonly busy = input(false);
  readonly error = input('');
  readonly signed = output<string>();
  readonly closed = output<void>();
  password = '';

  submit() {
    if (!this.password) return;
    this.signed.emit(this.password);
  }
}
