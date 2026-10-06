import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'ui-button',
  host: { class: 'inline-flex' },
  templateUrl: './ui-button.component.html',
})
export class UiButton {
  readonly type = input<'button' | 'submit'>('button');
  readonly variant = input<'primary' | 'ghost' | 'danger'>('primary');
  readonly disabled = input(false);
  readonly classes = computed(() => {
    const base =
      'inline-flex w-full min-h-10 cursor-pointer items-center justify-center rounded-sm px-3.5 py-2 text-sm transition duration-200 hover:-translate-y-px active:translate-y-0 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:translate-y-0';
    const variant = {
      primary: 'bg-clinic font-bold text-white hover:bg-clinic-deep',
      ghost: 'bg-white font-normal text-ink ring-1 ring-rule hover:bg-mist',
      danger: 'bg-pulse font-bold text-white hover:bg-pulse-deep',
    }[this.variant()];
    return `${base} ${variant}`;
  });
}
