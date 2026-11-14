import { Component, input, signal } from '@angular/core';
import { UiIcon } from './ui-icon.component';
import { UiModal } from './ui-modal.component';

// A visible "Ayuda" button next to a screen's title: what the screen shows and why lives in the dialog,
// so the explanation does not take room from the screen itself.
@Component({
  selector: 'ui-help',
  imports: [UiIcon, UiModal],
  template: `
    <button type="button" class="help-pill" aria-haspopup="dialog" [attr.aria-label]="'Ayuda: ' + title()" (click)="open.set(true)">
      <ui-icon name="help" [size]="18" />Ayuda
    </button>
    <ui-modal [open]="open()" [title]="title()" (closed)="open.set(false)">
      <div class="help-copy"><ng-content /></div>
    </ui-modal>
  `,
  styles: `
    :host {
      display: inline-flex;
    }

    .help-pill {
      display: inline-flex;
      min-height: 2.25rem;
      align-items: center;
      gap: 0.375rem;
      border-radius: 999px;
      padding: 0.3rem 0.875rem 0.3rem 0.625rem;
      background: var(--color-clinic-wash);
      color: var(--color-clinic-deep);
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s ease, transform 0.2s ease;
    }

    .help-pill:hover {
      background: color-mix(in srgb, var(--color-clinic-wash) 70%, var(--color-clinic) 30%);
      transform: translateY(-1px);
    }

    .help-pill:focus-visible {
      outline: 2px solid var(--color-clinic);
      outline-offset: 2px;
    }

    .help-copy {
      display: grid;
      gap: 0.75rem;
      color: rgb(14 42 34 / 0.78);
      font-size: 0.9375rem;
      line-height: 1.55;
    }

    .help-copy ::ng-deep p {
      margin: 0;
    }

    .help-copy ::ng-deep strong {
      color: var(--color-clinic-deep);
    }

    .help-copy ::ng-deep h3 {
      margin: 0.5rem 0 0;
      font-weight: 700;
      color: var(--color-clinic-deep);
    }

    .help-copy ::ng-deep em {
      color: var(--color-clinic);
      font-style: normal;
      font-weight: 500;
    }
  `,
})
export class UiHelp {
  readonly title = input('Ayuda');
  readonly open = signal(false);
}
