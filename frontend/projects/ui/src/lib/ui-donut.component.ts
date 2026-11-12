import { Component, computed, input, signal } from '@angular/core';
import { ChartSlice, chartColor, formatCount } from './ui-chart';

const radius = 42;
const circumference = 2 * Math.PI * radius;
const gap = 1.5;
const percent = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 1 });

// Parts of one whole, with the total in the middle and a legend that carries every value and share.
@Component({
  selector: 'ui-donut',
  template: `
    <div class="wrap">
      <svg viewBox="0 0 100 100" role="img" [attr.aria-label]="label()" (mouseleave)="active.set(null)">
        <circle class="track" cx="50" cy="50" [attr.r]="radius" />
        @for (arc of arcs(); track arc.label) {
          <circle
            cx="50" cy="50" [attr.r]="radius" fill="none" stroke-width="13"
            [attr.stroke]="arc.color"
            [attr.stroke-dasharray]="arc.dash"
            [attr.stroke-dashoffset]="arc.offset"
            [attr.opacity]="active() === null || active() === $index ? 1 : 0.35"
            transform="rotate(-90 50 50)"
            (mouseenter)="active.set($index)"
          />
        }
        <text x="50" y="49" text-anchor="middle" class="total" [attr.font-size]="centerSize()">{{ center() }}</text>
        <text x="50" y="61" text-anchor="middle" class="caption">{{ caption() }}</text>
      </svg>
      <ul class="legend">
        @for (arc of arcs(); track arc.label) {
          <li [class.is-dim]="active() !== null && active() !== $index" (mouseenter)="active.set($index)" (mouseleave)="active.set(null)">
            <span class="swatch" [style.background]="arc.color"></span>
            <span class="name">{{ arc.label }}</span>
            <span class="value">{{ format()(arc.value) }}</span>
            <span class="share">{{ arc.share }} %</span>
          </li>
        }
      </ul>
    </div>
  `,
  styles: `
    :host { display: block; min-width: 0; }
    .wrap { display: flex; flex-wrap: wrap; align-items: center; gap: 1.25rem; }
    svg { width: 9.5rem; height: 9.5rem; flex: none; }
    .track { fill: none; stroke: var(--color-mist); stroke-width: 13; }
    circle { transition: opacity 0.2s ease; cursor: default; }
    .total { font-weight: 750; fill: var(--color-ink); letter-spacing: -0.03em; }
    .caption { font-size: 7px; fill: rgb(14 42 34 / 0.55); }
    .legend { display: grid; flex: 1 1 10rem; gap: 0.5rem; min-width: 0; font-size: 0.875rem; }
    .legend li { display: flex; align-items: center; gap: 0.5rem; transition: opacity 0.2s ease; }
    .legend li.is-dim { opacity: 0.45; }
    .swatch { width: 0.7rem; height: 0.7rem; flex: none; border-radius: 0.2rem; }
    .name { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .value { margin-left: auto; font-weight: 700; font-variant-numeric: tabular-nums; }
    .share { width: 3.2rem; text-align: right; font-size: 0.8125rem; color: rgb(14 42 34 / 0.55); font-variant-numeric: tabular-nums; }
  `,
})
export class UiDonut {
  readonly slices = input<ChartSlice[]>([]);
  readonly label = input('Reparto');
  readonly center = input('');
  readonly caption = input('');
  readonly format = input<(value: number) => string>(formatCount);
  readonly radius = radius;
  readonly active = signal<number | null>(null);
  // Shrinks the middle figure so a long amount like $ 120.000 stays inside the ring.
  readonly centerSize = computed(() => { const length = this.center().length; return length <= 5 ? 15 : length <= 8 ? 12 : 10; });

  readonly arcs = computed(() => {
    const slices = this.slices().filter((slice) => slice.value > 0);
    const total = slices.reduce((sum, slice) => sum + slice.value, 0);
    let start = 0;
    return slices.map((slice) => {
      const length = total ? (slice.value / total) * circumference : 0;
      const visible = Math.max(0.5, length - (slices.length > 1 ? gap : 0));
      const arc = {
        label: slice.label,
        value: slice.value,
        color: chartColor(slice.tone),
        share: percent.format(total ? (slice.value / total) * 100 : 0),
        dash: `${visible} ${circumference - visible}`,
        offset: -start,
      };
      start += length;
      return arc;
    });
  });
}
