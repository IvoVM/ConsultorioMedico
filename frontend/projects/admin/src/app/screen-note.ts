import { Component, HostListener, input, signal } from '@angular/core';
import { UiModal } from 'ui';

@Component({
  selector: 'screen-note',
  imports: [UiModal],
  host: { class: 'screen-help' },
  template: `
    <button type="button" class="screen-help-button font-serif" aria-label="Ayuda de esta pantalla" aria-haspopup="dialog" (click)="open.set(true)">?</button>
    <ui-modal [open]="open()" title="Esta pantalla" (closed)="open.set(false)">
      <div class="screen-help-copy">
        <p>{{ loading() }}</p>
        <p><span>Por qué lo editás.</span> {{ reason() }}</p>
        <p><span>Si no lo cargás.</span> {{ skip() }}</p>
        <p><span>Quién más puede cargarlo.</span> {{ others() }}</p>
      </div>
    </ui-modal>
  `,
  styles: `
    :host {
      position: fixed;
      z-index: 40;
      right: 1.25rem;
      top: 50%;
      margin-top: -1.625rem;
    }

    .screen-help-button {
      display: grid;
      width: 3.25rem;
      height: 3.25rem;
      place-items: center;
      border-radius: 999px;
      background: var(--color-clinic);
      color: white;
      font-size: 1.75rem;
      line-height: 1;
      cursor: pointer;
      box-shadow: 0 10px 24px rgb(23 51 44 / 0.18);
    }

    .screen-help-button:hover {
      background: var(--color-clinic-deep);
    }

    .screen-help-copy {
      display: grid;
      gap: 0.85rem;
      color: rgb(23 51 44 / 0.82);
      font-size: 0.95rem;
      line-height: 1.5;
    }

    .screen-help-copy span {
      font-weight: 700;
      color: var(--color-clinic-deep);
    }

    .screen-help-copy p {
      margin: 0;
    }
  `,
})
export class ScreenNote {
  readonly loading = input.required<string>();
  readonly reason = input.required<string>();
  readonly skip = input.required<string>();
  readonly others = input.required<string>();
  readonly open = signal(false);

  @HostListener('document:keydown.escape')
  close() {
    this.open.set(false);
  }
}
