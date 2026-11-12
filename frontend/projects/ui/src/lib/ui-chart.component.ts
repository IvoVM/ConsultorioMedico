import { Component, computed, input, signal } from '@angular/core';
import { ChartPoint, ChartSeries, chartColor, formatCount, hostWidth, niceMax } from './ui-chart';

export enum ChartKind {
  Bars = 'bars',
  Lines = 'lines',
}

const pad = { left: 58, right: 10, top: 12, bottom: 26 };
const gap = 2;
const maxBar = 24;

type Segment = { d: string; color: string };
type Column = { x: number; band: number; segments: Segment[] };
type Line = { color: string; line: string; area: string; dots: { x: number; y: number }[] };

// Stacked columns (one per slot, a segment per series) or lines over the same slots, on one value axis.
@Component({
  selector: 'ui-chart',
  template: `
    @if (series().length > 1) {
      <ul class="legend" aria-hidden="true">
        @for (item of series(); track item.key) {
          <li><span class="swatch" [class.is-line]="kind() === kinds.Lines" [style.background]="color(item)"></span>{{ item.label }}</li>
        }
      </ul>
    }
    <div class="plot" [style.height.px]="height()">
      @if (layout(); as view) {
        <svg [attr.width]="view.width" [attr.height]="height()" role="img" [attr.aria-label]="label()" (mouseleave)="active.set(null)">
          @for (tick of view.ticks; track $index) {
            <line class="grid" [attr.x1]="pad.left" [attr.x2]="view.width - pad.right" [attr.y1]="tick.y" [attr.y2]="tick.y" />
            <text class="tick" [attr.x]="pad.left - 8" [attr.y]="tick.y" text-anchor="end" dominant-baseline="middle">{{ tick.label }}</text>
          }
          @for (tick of view.xTicks; track tick.index) {
            <text class="tick" [attr.x]="tick.x" [attr.y]="height() - 6" text-anchor="middle">{{ tick.label }}</text>
          }
          @if (active() !== null) {
            <rect class="hover-band" [attr.x]="view.columns[active()!].x - view.columns[active()!].band / 2" [attr.y]="pad.top" [attr.width]="view.columns[active()!].band" [attr.height]="view.plotHeight" />
          }
          @for (column of view.columns; track $index) {
            @for (segment of column.segments; track $index) {
              <path [attr.d]="segment.d" [attr.fill]="segment.color" />
            }
          }
          @for (line of view.lines; track $index) {
            <path [attr.d]="line.area" [attr.fill]="line.color" fill-opacity="0.1" />
            <path [attr.d]="line.line" [attr.stroke]="line.color" fill="none" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" />
            @if (active() !== null) {
              <circle class="dot" [attr.cx]="line.dots[active()!].x" [attr.cy]="line.dots[active()!].y" r="4.5" [attr.fill]="line.color" />
            }
          }
          @for (column of view.columns; track $index) {
            <rect class="hit" [attr.x]="column.x - column.band / 2" [attr.y]="0" [attr.width]="column.band" [attr.height]="height()" (mouseenter)="active.set($index)" />
          }
        </svg>
        @if (active() !== null) {
          @let point = points()[active()!];
          <div class="tip" role="status" [style.left.px]="tipLeft(view.columns[active()!].x, view.width)">
            <p class="tip-title">{{ point.hint ?? point.label }}</p>
            @for (item of series(); track item.key) {
              <p class="tip-row"><span class="swatch" [style.background]="color(item)"></span>{{ item.label }}<b>{{ format()(point.values[item.key] ?? 0) }}</b></p>
            }
            @if (kind() === kinds.Bars && series().length > 1) {
              <p class="tip-row is-total">Total<b>{{ format()(total(point)) }}</b></p>
            }
          </div>
        }
      }
    </div>
    <div class="sr-only">
    <table>
      <caption>{{ label() }}</caption>
      <thead><tr><th scope="col">Período</th>@for (item of series(); track item.key) { <th scope="col">{{ item.label }}</th> }</tr></thead>
      <tbody>
        @for (point of points(); track $index) {
          <tr><th scope="row">{{ point.hint ?? point.label }}</th>@for (item of series(); track item.key) { <td>{{ format()(point.values[item.key] ?? 0) }}</td> }</tr>
        }
      </tbody>
    </table>
    </div>
  `,
  styles: `
    :host { display: block; position: relative; min-width: 0; }
    .plot { position: relative; width: 100%; }
    svg { display: block; overflow: visible; }
    .legend { display: flex; flex-wrap: wrap; gap: 0.35rem 1rem; margin-bottom: 0.75rem; font-size: 0.8125rem; color: rgb(14 42 34 / 0.7); }
    .legend li { display: inline-flex; align-items: center; gap: 0.4rem; }
    .swatch { display: inline-block; width: 0.65rem; height: 0.65rem; border-radius: 0.2rem; flex: none; }
    .swatch.is-line { height: 0.2rem; border-radius: 1rem; }
    .grid { stroke: var(--color-rule); stroke-width: 1; }
    .tick { fill: rgb(14 42 34 / 0.5); font-size: 11px; font-variant-numeric: tabular-nums; }
    .hover-band { fill: var(--color-mist); }
    .hit { fill: transparent; cursor: crosshair; }
    .dot { stroke: white; stroke-width: 2; }
    .tip {
      position: absolute; top: 0; z-index: 2; min-width: 10rem; transform: translateX(-50%); pointer-events: none;
      border-radius: 0.75rem; background: white; padding: 0.6rem 0.75rem; box-shadow: var(--shadow-lift); font-size: 0.8125rem;
    }
    .tip-title { margin-bottom: 0.3rem; font-weight: 700; }
    .tip-row { display: flex; align-items: center; gap: 0.45rem; color: rgb(14 42 34 / 0.7); }
    .tip-row b { margin-left: auto; padding-left: 0.75rem; color: var(--color-ink); font-variant-numeric: tabular-nums; }
    .tip-row.is-total { margin-top: 0.25rem; border-top: 1px solid var(--color-rule); padding-top: 0.25rem; }
  `,
})
export class UiChart {
  readonly kind = input(ChartKind.Bars);
  readonly series = input<ChartSeries[]>([]);
  readonly points = input<ChartPoint[]>([]);
  readonly label = input('Gráfico');
  readonly height = input(220);
  readonly format = input<(value: number) => string>(formatCount);
  // A shorter format for the value axis, when the tooltip's full one would not fit beside the plot.
  readonly axisFormat = input<((value: number) => string) | null>(null);
  readonly kinds = ChartKind;
  readonly pad = pad;
  readonly active = signal<number | null>(null);
  private readonly width = hostWidth();

  readonly color = (item: ChartSeries) => chartColor(item.tone);

  readonly total = (point: ChartPoint) => this.series().reduce((sum, item) => sum + (point.values[item.key] ?? 0), 0);

  readonly layout = computed(() => {
    const width = this.width();
    if (!width) return null;
    const points = this.points();
    const series = this.series();
    const bars = this.kind() === ChartKind.Bars;
    const plotWidth = Math.max(1, width - pad.left - pad.right);
    const plotHeight = this.height() - pad.top - pad.bottom;
    const peaks = points.map((point) => (bars ? this.total(point) : Math.max(0, ...series.map((item) => point.values[item.key] ?? 0))));
    const max = niceMax(Math.max(0, ...peaks));
    const y = (value: number) => pad.top + plotHeight - (value / max) * plotHeight;
    const band = plotWidth / Math.max(1, points.length);
    const center = (index: number) => pad.left + band * (index + 0.5);
    const every = Math.max(1, Math.ceil(points.length / Math.max(1, Math.floor(plotWidth / 54))));
    const barWidth = Math.min(maxBar, Math.max(2, band - gap * 2));

    const columns: Column[] = points.map((point, index) => {
      const x = center(index);
      if (!bars) return { x, band, segments: [] };
      const visible = series.filter((item) => (point.values[item.key] ?? 0) > 0);
      let base = 0;
      const segments = visible.map((item, order) => {
        const value = point.values[item.key] ?? 0;
        const top = y(base + value);
        const bottom = y(base) - (order > 0 ? gap : 0);
        base += value;
        return { d: column(x - barWidth / 2, top, barWidth, Math.max(0, bottom - top), order === visible.length - 1), color: chartColor(item.tone) };
      });
      return { x, band, segments };
    });

    const lines: Line[] = bars
      ? []
      : series.map((item) => {
          const dots = points.map((point, index) => ({ x: center(index), y: y(point.values[item.key] ?? 0) }));
          const line = dots.map((dot, index) => `${index ? 'L' : 'M'}${dot.x},${dot.y}`).join(' ');
          const baseline = y(0);
          const area = dots.length ? `${line} L${dots[dots.length - 1].x},${baseline} L${dots[0].x},${baseline} Z` : '';
          return { color: chartColor(item.tone), line, area, dots };
        });

    return {
      width,
      plotHeight,
      columns,
      lines,
      ticks: [0, max / 2, max].map((value) => ({ y: y(value), label: (this.axisFormat() ?? this.format())(value) })),
      xTicks: points.map((point, index) => ({ index, x: center(index), label: point.label })).filter((tick) => tick.index % every === 0),
    };
  });

  tipLeft(x: number, width: number) {
    return Math.min(Math.max(x, 84), width - 84);
  }
}

// A column with a 4px rounded data end on top and a square foot on the baseline.
function column(x: number, y: number, width: number, height: number, rounded: boolean) {
  const r = rounded ? Math.min(4, height, width / 2) : 0;
  return `M${x},${y + height} V${y + r} Q${x},${y} ${x + r},${y} H${x + width - r} Q${x + width},${y} ${x + width},${y + r} V${y + height} Z`;
}
