import { Component, input, model, signal } from '@angular/core';
import { UiModal } from './ui-modal.component';

// The privacy notice as the API serves it; the version is what the person agrees to.
export interface ConsentNotice {
  version: string;
  clinic: string;
  sections: { title: string; body: string }[];
}

// A checkbox that agrees to the clinic's privacy notice, with a link that opens the full text. A patient ticks it
// for themselves; front desk ticks it once they told the patient the notice (staff mode).
@Component({
  selector: 'ui-consent',
  imports: [UiModal],
  template: `
    <label class="row">
      <input type="checkbox" [checked]="accepted()" [disabled]="!notice()" (change)="accepted.set($any($event.target).checked)" />
      <span>
        {{ label() }}
        <button type="button" class="link" [disabled]="!notice()" (click)="reading.set(true)">Leer el aviso de privacidad</button>
      </span>
    </label>
    @if (reading() && notice(); as text) {
      <ui-modal [open]="true" [title]="'Aviso de privacidad · ' + text.clinic" wide (closed)="reading.set(false)">
        <div class="notice">
          @for (section of text.sections; track section.title) {
            <h3>{{ section.title }}</h3>
            <p>{{ section.body }}</p>
          }
          <p class="version">Versión {{ text.version }}</p>
        </div>
      </ui-modal>
    }
  `,
  styles: `
    :host {
      display: block;
    }

    .row {
      display: flex;
      align-items: flex-start;
      gap: 0.6rem;
      font-size: 0.875rem;
      line-height: 1.4;
      color: color-mix(in srgb, var(--color-ink) 80%, transparent);
      cursor: pointer;
    }

    .row input {
      margin-top: 0.2rem;
      flex: none;
    }

    .link {
      display: inline;
      margin-left: 0.25rem;
      font-weight: 600;
      color: var(--color-clinic);
      text-decoration: underline;
      text-underline-offset: 2px;
      cursor: pointer;
    }

    .notice h3 {
      margin-top: 1.1rem;
      font-weight: 600;
      color: var(--color-ink);
    }

    .notice h3:first-child {
      margin-top: 0;
    }

    .notice p {
      margin-top: 0.35rem;
      font-size: 0.9rem;
      line-height: 1.55;
      color: color-mix(in srgb, var(--color-ink) 78%, transparent);
    }

    .notice .version {
      margin-top: 1.25rem;
      font-size: 0.75rem;
    }
  `,
})
export class UiConsent {
  readonly notice = input<ConsentNotice | null>(null);
  readonly label = input('Leí el aviso de privacidad y acepto que el consultorio use mis datos para darme turnos y atenderme.');
  readonly accepted = model(false);
  readonly reading = signal(false);
}

export const staffConsentLabel = 'Le conté al paciente el aviso de privacidad y aceptó que carguemos sus datos.';
