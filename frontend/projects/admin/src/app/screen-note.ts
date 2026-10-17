import { Component, HostListener, computed, input, signal } from '@angular/core';
import { UiModal } from 'ui';

@Component({
  selector: 'screen-note',
  imports: [UiModal],
  host: { class: 'screen-help' },
  template: `
    <button type="button" class="screen-help-button font-serif" aria-label="Ayuda de esta pantalla" aria-haspopup="dialog" (click)="open.set(true)">?</button>
    <ui-modal [open]="open()" [title]="view().screen" (closed)="dismiss()">
      <div class="screen-help-copy">
        <p>@for (part of pieces(view().loading); track $index) {@if (part.mark) {<em class="section-ref">{{ part.text }}</em>} @else {{{part.text}}}}</p>
        <p><span>Por qué lo editás: </span>@for (part of pieces(view().reason); track $index) {@if (part.mark) {<em class="section-ref">{{ part.text }}</em>} @else {{{part.text}}}}</p>
        <p><span>Si no lo cargás: </span>@for (part of pieces(view().skip); track $index) {@if (part.mark) {<em class="section-ref">{{ part.text }}</em>} @else {{{part.text}}}}</p>
        @if (view().others) {
          <p><span>Quién más puede cargarlo: </span>@for (part of pieces(view().others); track $index) {@if (part.mark) {<em class="section-ref">{{ part.text }}</em>} @else {{{part.text}}}}</p>
        }
      </div>
      @if (nextScreen()) {
        <div class="help-nav" [class.is-back]="page() === 1">
          @if (page() === 0) {
            <button type="button" class="help-switch" (click)="page.set(1)">{{ nextScreen() }} <span aria-hidden="true">→</span></button>
          } @else {
            <button type="button" class="help-switch" (click)="page.set(0)"><span aria-hidden="true">←</span> {{ screen() }}</button>
          }
        </div>
      }
    </ui-modal>
  `,
  styles: `
    :host {
      position: fixed;
      z-index: 40;
      top: 0.7rem;
      right: 1.5rem;
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
      font-size: 1.25rem;
      line-height: 1.45;
    }

    .screen-help-copy span {
      font-weight: 700;
      color: var(--color-clinic-deep);
    }

    .screen-help-copy p {
      margin: 0;
    }

    .section-ref {
      color: var(--color-clinic);
      font-style: normal;
      font-weight: 500;
    }

    .help-nav {
      display: flex;
      justify-content: flex-end;
      margin-top: 1.5rem;
    }

    .help-nav.is-back {
      justify-content: flex-start;
    }

    .help-switch {
      display: inline-flex;
      min-height: 2.75rem;
      cursor: pointer;
      align-items: center;
      gap: 0.5rem;
      border-radius: 2px;
      padding: 0.4rem 0.9rem;
      color: var(--color-clinic-deep);
      font-size: 1.125rem;
      font-weight: 500;
      box-shadow: inset 0 0 0 1.5px var(--color-clinic);
    }

    .help-switch:hover {
      background: var(--color-clinic-wash);
    }
  `,
})
export class ScreenNote {
  readonly screen = input.required<string>();
  readonly loading = input.required<string>();
  readonly reason = input.required<string>();
  readonly skip = input.required<string>();
  readonly others = input('');
  readonly nextScreen = input('');
  readonly nextLoading = input('');
  readonly nextReason = input('');
  readonly nextSkip = input('');
  readonly nextOthers = input('');
  readonly open = signal(false);
  readonly page = signal(0);
  readonly view = computed(() =>
    this.page() === 0 || !this.nextScreen()
      ? { screen: this.screen(), loading: this.loading(), reason: this.reason(), skip: this.skip(), others: this.others() }
      : { screen: this.nextScreen(), loading: this.nextLoading(), reason: this.nextReason(), skip: this.nextSkip(), others: this.nextOthers() },
  );

  @HostListener('document:keydown.escape')
  close() {
    this.dismiss();
  }

  dismiss() {
    this.open.set(false);
    this.page.set(0);
  }

  pieces(value: string) {
    return value.split(/\[\[(.+?)\]\]/).map((text, index) => ({ text, mark: index % 2 === 1 }));
  }
}
