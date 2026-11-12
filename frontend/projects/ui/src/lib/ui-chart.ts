import { DestroyRef, ElementRef, inject, signal } from '@angular/core';
import { UiTone } from './ui-board.component';

// One measured thing drawn in a chart; the tone picks its color from the chart tokens in theme.css.
export type ChartSeries = { key: string; label: string; tone: UiTone };

// One slot along the x axis: a short tick label, a longer one for the tooltip, and a value per series key.
export type ChartPoint = { label: string; hint?: string; values: Record<string, number> };

export type ChartSlice = { label: string; value: number; tone: UiTone };

export const chartColor = (tone: UiTone) => `var(--chart-${tone})`;

const numberFormat = new Intl.NumberFormat('es-AR');
export const formatCount = (value: number) => numberFormat.format(value);

// Rounds the top of an axis up to 1, 2, 2.5 or 5 times a power of ten, so the ticks read as clean numbers.
export function niceMax(value: number) {
  if (!(value > 0)) return 4;
  const magnitude = Math.pow(10, Math.floor(Math.log10(value)));
  for (const step of [1, 2, 2.5, 5, 10]) if (step * magnitude >= value) return step * magnitude;
  return 10 * magnitude;
}

// The rendered width of the host element, kept current while the layout changes.
export function hostWidth() {
  const host = inject(ElementRef).nativeElement as HTMLElement;
  const width = signal(0);
  if (typeof ResizeObserver === 'function') {
    const observer = new ResizeObserver((entries) => width.set(Math.floor(entries[0].contentRect.width)));
    observer.observe(host);
    inject(DestroyRef).onDestroy(() => observer.disconnect());
  }
  return width;
}
