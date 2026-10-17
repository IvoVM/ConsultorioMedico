import { Component, input } from '@angular/core';

export type SortDir = 'asc' | 'desc';
export type SortState = { key: string; dir: SortDir };

export function nextSort(current: SortState, key: string): SortState {
  if (current.key !== key) return { key, dir: 'asc' };
  return { key, dir: current.dir === 'asc' ? 'desc' : 'asc' };
}

export function compareSort(a: string | number | null | undefined, b: string | number | null | undefined, dir: SortDir) {
  const left = a ?? '';
  const right = b ?? '';
  const result =
    typeof left === 'number' && typeof right === 'number'
      ? left - right
      : String(left).localeCompare(String(right), 'es', { numeric: true, sensitivity: 'base' });
  return dir === 'asc' ? result : -result;
}

@Component({
  selector: 'button[sortCol]',
  host: { class: 'sort-col' },
  template: `<ng-content />@if (on()) {<span aria-hidden="true">{{ dir() === 'asc' ? '↑' : '↓' }}</span>}`,
})
export class SortCol {
  readonly on = input(false);
  readonly dir = input<SortDir>('asc');
}
