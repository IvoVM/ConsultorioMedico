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
      min-width: 10.5rem;
      min-height: 2.75rem;
      cursor: pointer;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      border-radius: 2px;
      padding: 0.5rem 0.875rem;
      font-size: 1.125rem;
      font-weight: 700;
      line-height: 1.4;
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
    }

    .btn-primary:hover:not(:disabled) {
      background: var(--color-clinic-deep);
    }

    .btn-secondary {
      background: white;
      color: var(--color-ink);
      box-shadow: inset 0 0 0 1px var(--color-rule);
    }

    .btn-secondary:hover:not(:disabled) {
      background: var(--color-mist);
    }

    .btn-error {
      background: var(--color-pulse);
      color: white;
    }

    .btn-error:hover:not(:disabled) {
      background: var(--color-pulse-deep);
    }
  `,
})
export class UiButton {
  readonly type = input<'button' | 'submit'>('button');
  readonly variant = input<'primary' | 'secondary' | 'error' | 'ghost' | 'danger' | 'outlined'>('primary');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly busy = computed(() => this.disabled() || this.loading());
  readonly classes = computed(() => {
    const variant = this.variant();
    if (variant === 'ghost' || variant === 'outlined') return 'btn-secondary';
    if (variant === 'danger') return 'btn-error';
    return `btn-${variant}`;
  });
}
