import { Component, computed, input } from '@angular/core';

export type HeatmapRow = { label: string; values: number[] };

// A grid of counts, one hue from pale to deep: the darker the cell, the busier that slot.
@Component({
  selector: 'ui-heatmap',
  template: `
    <div class="scroll">
      <table [attr.aria-label]="label()">
        <thead>
          <tr>
            <th scope="col"><span class="sr-only">Día</span></th>
            @for (column of columns(); track $index) {
              <th scope="col" class="col">{{ column }}</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (row of rows(); track row.label) {
            <tr>
              <th scope="row" class="day">{{ row.label }}</th>
              @for (value of row.values; track $index) {
                <td [style.background]="fill(value)" [attr.title]="row.label + ' ' + columns()[$index] + ': ' + value">
                  <span class="sr-only">{{ value }}</span>
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
    <div class="scale" aria-hidden="true">
      <span>Menos</span>
      @for (step of steps; track step) {
        <i [style.background]="mix(step)"></i>
      }
      <span>Más</span>
    </div>
  `,
  styles: `
    :host { display: block; min-width: 0; }
    .scroll { position: relative; max-width: 100%; overflow-x: auto; }
    table { border-collapse: separate; border-spacing: 3px; width: 100%; }
    th { font-size: 0.6875rem; font-weight: 600; color: rgb(14 42 34 / 0.5); font-variant-numeric: tabular-nums; }
    .col { min-width: 1.6rem; text-align: center; }
    .day { padding-right: 0.4rem; text-align: left; white-space: nowrap; }
    td { height: 1.6rem; border-radius: 0.35rem; transition: transform 0.2s ease; }
    td:hover { transform: scale(1.12); box-shadow: 0 0 0 2px white, 0 0 0 3px var(--color-clinic); }
    .scale { display: flex; align-items: center; justify-content: flex-end; gap: 3px; margin-top: 0.6rem; font-size: 0.6875rem; color: rgb(14 42 34 / 0.5); }
    .scale span { margin: 0 0.3rem; }
    .scale i { width: 1rem; height: 0.6rem; border-radius: 0.2rem; }
  `,
})
export class UiHeatmap {
  readonly rows = input<HeatmapRow[]>([]);
  readonly columns = input<string[]>([]);
  readonly label = input('Mapa de calor');
  readonly steps = [0.1, 0.35, 0.6, 0.85, 1];
  private readonly max = computed(() => Math.max(0, ...this.rows().flatMap((row) => row.values)));

  mix(share: number) {
    return `color-mix(in oklab, var(--color-clinic) ${Math.round(share * 100)}%, var(--color-mist))`;
  }

  fill(value: number) {
    return value && this.max() ? this.mix(0.12 + 0.88 * (value / this.max())) : 'var(--color-mist)';
  }
}
