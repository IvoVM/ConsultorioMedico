import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { UiIcon } from './ui-icon.component';
import { UiTone } from './ui-board.component';

@Component({
  selector: 'ui-stat',
  imports: [UiIcon],
  host: { class: 'ui-stat surface surface-lift' },
  template: `
    <div class="flex items-center gap-2.5 text-sm font-medium text-ink/65">
      <span class="glyph"><ui-icon [name]="icon()" [size]="18" /></span>
      <span class="truncate">{{ label() }}</span>
    </div>
    <div class="mt-3 flex flex-wrap items-baseline gap-x-3 gap-y-1">
      <span class="value tabular-nums">{{ shown() }}{{ suffix() }}</span>
      @if (note()) {
        <span class="chip" [class.is-ok]="tone() === tones.Ok" [class.is-warn]="tone() === tones.Warn" [class.is-bad]="tone() === tones.Bad">{{ note() }}</span>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      padding: 1.1rem 1.25rem 1.2rem;
    }

    .glyph {
      display: grid;
      width: 2rem;
      height: 2rem;
      place-items: center;
      border-radius: 0.65rem;
      background: var(--color-clinic-wash);
      color: var(--color-clinic);
      transition: transform 0.35s cubic-bezier(0.16, 0.84, 0.32, 1);
    }

    :host(:hover) .glyph {
      transform: rotate(-8deg) scale(1.08);
    }

    .value {
      font-size: 2rem;
      font-weight: 750;
      line-height: 1;
      letter-spacing: -0.04em;
    }
  `,
})
export class UiStat {
  readonly label = input('');
  readonly icon = input('insights');
  readonly value = input<number | string>(0);
  readonly suffix = input('');
  readonly note = input('');
  readonly tone = input<UiTone>(UiTone.Muted);
  readonly tones = UiTone;
  readonly shown = signal<string>('0');
  private frame = 0;

  constructor() {
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(this.frame));
    effect(() => {
      const value = this.value();
      cancelAnimationFrame(this.frame);
      if (typeof value !== 'number' || !Number.isFinite(value)) {
        this.shown.set(String(value));
        return;
      }
      const reduce = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
      const decimals = Number.isInteger(value) ? 0 : 1;
      if (reduce || value === 0) {
        this.shown.set(value.toFixed(decimals));
        return;
      }
      const start = performance.now();
      const duration = 900;
      const step = (now: number) => {
        const progress = Math.min(1, (now - start) / duration);
        const eased = 1 - Math.pow(1 - progress, 3);
        this.shown.set((value * eased).toFixed(decimals));
        if (progress < 1) this.frame = requestAnimationFrame(step);
      };
      this.frame = requestAnimationFrame(step);
    });
  }
}
