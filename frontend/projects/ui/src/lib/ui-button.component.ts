import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'ui-button',
  host: { class: 'inline-flex' },
  templateUrl: './ui-button.component.html',
  styles: `
    .btn-spin {
      width: 0.875rem;
      height: 0.875rem;
      flex: none;
      border: 2px solid currentColor;
      border-right-color: transparent;
      border-radius: 999px;
      animation: btn-spin 0.7s linear infinite;
    }

    @keyframes btn-spin {
      to {
        transform: rotate(360deg);
      }
    }
  `,
})
export class UiButton {
  readonly type = input<'button' | 'submit'>('button');
  readonly variant = input<'primary' | 'ghost' | 'danger'>('primary');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly busy = computed(() => this.disabled() || this.loading());
  readonly classes = computed(() => {
    const fade = this.loading() ? 'disabled:opacity-100' : 'disabled:opacity-50';
    const base =
      `inline-flex w-full min-h-10 cursor-pointer items-center justify-center gap-2 rounded-sm px-3.5 py-2 text-sm transition duration-200 hover:-translate-y-px active:translate-y-0 active:scale-[0.98] disabled:cursor-not-allowed ${fade} disabled:hover:translate-y-0`;
    const variant = {
      primary: 'bg-clinic font-bold text-white hover:bg-clinic-deep',
      ghost: 'bg-white font-normal text-ink ring-1 ring-rule hover:bg-mist',
      danger: 'bg-pulse font-bold text-white hover:bg-pulse-deep',
    }[this.variant()];
    return `${base} ${variant}`;
  });
}
