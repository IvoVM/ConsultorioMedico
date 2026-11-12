import { Component, computed, input } from '@angular/core';
import { UiTone } from './ui-board.component';
import { chartColor, formatCount } from './ui-chart';

export type BarListRow = { label: string; value: number; note?: string };

// A ranking: one horizontal bar per row, measured against the largest row.
@Component({
  selector: 'ui-bar-list',
  template: `
    <ol class="rows" [attr.aria-label]="label()">
      @for (row of rows(); track row.label) {
        <li>
          <div class="line">
            <span class="name">{{ row.label }}</span>
            <span class="value">{{ format()(row.value) }}@if (row.note) {<small>{{ row.note }}</small>}</span>
          </div>
          <div class="track" aria-hidden="true"><span class="bar" [style.width.%]="share(row.value)" [style.background]="color()"></span></div>
        </li>
      }
    </ol>
  `,
  styles: `
    :host { display: block; min-width: 0; }
    .rows { display: grid; gap: 0.8rem; }
    .line { display: flex; align-items: baseline; justify-content: space-between; gap: 0.75rem; font-size: 0.875rem; }
    .name { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-weight: 600; }
    .value { flex: none; font-weight: 700; font-variant-numeric: tabular-nums; }
    .value small { margin-left: 0.4rem; font-size: 0.75rem; font-weight: 500; color: rgb(14 42 34 / 0.55); }
    .track { margin-top: 0.35rem; height: 0.5rem; border-radius: 999px; background: var(--color-mist); }
    .bar { display: block; height: 100%; min-width: 0.5rem; border-radius: 999px; transition: width 0.6s cubic-bezier(0.16, 0.84, 0.32, 1); }
  `,
})
export class UiBarList {
  readonly rows = input<BarListRow[]>([]);
  readonly tone = input(UiTone.Muted);
  readonly label = input('Ranking');
  readonly format = input<(value: number) => string>(formatCount);
  readonly color = computed(() => chartColor(this.tone()));
  private readonly max = computed(() => Math.max(0, ...this.rows().map((row) => row.value)));

  share(value: number) {
    return this.max() ? (value / this.max()) * 100 : 0;
  }
}
