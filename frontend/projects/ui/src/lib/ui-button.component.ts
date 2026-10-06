import { Component, computed, input } from '@angular/core';
import { UiSpinner } from './ui-spinner.component';

@Component({
  selector: 'ui-button',
  imports: [UiSpinner],
  host: { class: 'inline-flex' },
  templateUrl: './ui-button.component.html',
  styles: `
    button {
      display: inline-flex;
      width: 100%;
      min-height: 2.5rem;
      cursor: pointer;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      border-radius: 2px;
      padding: 0.5rem 0.875rem;
      font-size: 0.875rem;
      line-height: 1.25rem;
      transition:
        background-color 0.2s ease,
        color 0.2s ease,
        box-shadow 0.2s ease,
        transform 0.2s ease;
    }

    button:hover:not(:disabled) {
      transform: translateY(-1px);
    }

    button:active:not(:disabled) {
      transform: none;
      scale: 0.98;
    }

    button:disabled {
      cursor: not-allowed;
      transform: none;
      background: var(--color-mist);
      color: rgb(23 51 44 / 0.4);
      box-shadow: inset 0 0 0 1px var(--color-rule);
    }

    button:disabled:hover,
    button:disabled:active {
      transform: none;
      background: var(--color-mist);
    }

    .btn-primary {
      background: var(--color-clinic);
      color: white;
      font-weight: 700;
    }

    .btn-primary:hover:not(:disabled) {
      background: var(--color-clinic-deep);
    }

    .btn-danger {
      background: var(--color-pulse);
      color: white;
      font-weight: 700;
    }

    .btn-danger:hover:not(:disabled) {
      background: var(--color-pulse-deep);
    }

    .btn-ghost {
      background: white;
      color: var(--color-ink);
      box-shadow: inset 0 0 0 1px var(--color-rule);
    }

    .btn-ghost:hover:not(:disabled) {
      background: var(--color-mist);
    }

    .btn-outlined {
      background: transparent;
      color: var(--color-clinic-deep);
      font-weight: 700;
      box-shadow: inset 0 0 0 1.5px var(--color-clinic);
    }

    .btn-outlined:hover:not(:disabled) {
      background: var(--color-clinic-wash);
    }

    .btn-outlined:disabled,
    .btn-outlined:disabled:hover {
      background: transparent;
      color: rgb(23 51 44 / 0.35);
      box-shadow: inset 0 0 0 1.5px var(--color-rule);
    }
  `,
})
export class UiButton {
  readonly type = input<'button' | 'submit'>('button');
  readonly variant = input<'primary' | 'ghost' | 'danger' | 'outlined'>('primary');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly busy = computed(() => this.disabled() || this.loading());
  readonly classes = computed(() => `btn-${this.variant()}`);
}
