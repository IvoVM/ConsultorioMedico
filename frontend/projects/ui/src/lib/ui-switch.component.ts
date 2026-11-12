import { Component, input, model } from '@angular/core';

// An on/off setting row: the label and its explanation on the left, the switch on the right.
@Component({
  selector: 'ui-switch',
  template: `
    <button
      type="button"
      role="switch"
      class="row"
      [attr.aria-checked]="checked()"
      [disabled]="disabled()"
      (click)="checked.set(!checked())"
    >
      <span class="text">
        <span class="label">{{ label() }}</span>
        @if (description()) {
          <span class="description">{{ description() }}</span>
        }
      </span>
      <span class="track" [class.is-on]="checked()" aria-hidden="true"><span class="thumb"></span></span>
    </button>
  `,
  styles: `
    :host {
      display: block;
    }

    .row {
      display: flex;
      width: 100%;
      align-items: center;
      justify-content: space-between;
      gap: 1.5rem;
      padding: 1rem 0;
      text-align: left;
      cursor: pointer;
    }

    .row:disabled {
      cursor: not-allowed;
      opacity: 0.5;
    }

    .row:focus-visible {
      outline: 2px solid var(--color-leaf);
      outline-offset: 4px;
      border-radius: 0.5rem;
    }

    .text {
      display: flex;
      min-width: 0;
      flex-direction: column;
      gap: 0.25rem;
    }

    .label {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--color-ink);
    }

    .description {
      font-size: 0.875rem;
      line-height: 1.5;
      color: rgb(14 42 34 / 0.65);
    }

    .track {
      position: relative;
      flex: none;
      width: 2.75rem;
      height: 1.5rem;
      border-radius: 999px;
      background: var(--color-rule);
      box-shadow: inset 0 0 0 1px rgb(14 42 34 / 0.08);
      transition: background-color 0.25s ease;
    }

    .track.is-on {
      background: var(--color-clinic);
    }

    .thumb {
      position: absolute;
      top: 0.1875rem;
      left: 0.1875rem;
      width: 1.125rem;
      height: 1.125rem;
      border-radius: 999px;
      background: white;
      box-shadow: 0 1px 3px rgb(14 42 34 / 0.3);
      transition: transform 0.25s cubic-bezier(0.16, 0.84, 0.32, 1);
    }

    .track.is-on .thumb {
      transform: translateX(1.25rem);
    }

    @media (prefers-reduced-motion: reduce) {
      .track,
      .thumb {
        transition: none;
      }
    }
  `,
})
export class UiSwitch {
  readonly label = input('');
  readonly description = input('');
  readonly disabled = input(false);
  readonly checked = model(false);
}
