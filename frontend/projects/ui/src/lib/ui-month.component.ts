import { Component, computed, input, output } from '@angular/core';
import { UiIcon } from './ui-icon.component';
import { localDate } from './request-tabs';

type Cell = { iso: string; day: number; open: boolean; past: boolean; today: boolean };

@Component({
  selector: 'ui-month',
  imports: [UiIcon],
  templateUrl: './ui-month.component.html',
  styleUrl: './ui-month.component.css',
})
export class UiMonth {
  readonly year = input.required<number>();
  readonly month = input.required<number>();
  readonly openDays = input<string[]>([]);
  readonly selected = input('');
  readonly loading = input(false);
  readonly pick = output<string>();
  readonly shift = output<number>();

  readonly title = computed(() => {
    const label = new Date(this.year(), this.month() - 1, 1).toLocaleDateString('es-AR', { month: 'long' });
    return `${label.charAt(0).toUpperCase()}${label.slice(1)} ${this.year()}`;
  });

  readonly canGoBack = computed(() => {
    const today = new Date();
    return this.year() > today.getFullYear() || (this.year() === today.getFullYear() && this.month() > today.getMonth() + 1);
  });

  readonly cells = computed(() => {
    const first = new Date(this.year(), this.month() - 1, 1);
    const lead = (first.getDay() + 6) % 7;
    const count = new Date(this.year(), this.month(), 0).getDate();
    const open = new Set(this.openDays().map((day) => day.slice(0, 10)));
    const today = localDate(new Date());
    const cells: Array<Cell | null> = Array.from({ length: lead }, () => null);
    for (let day = 1; day <= count; day++) {
      const iso = `${this.year()}-${String(this.month()).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      cells.push({ iso, day, open: open.has(iso), past: iso < today, today: iso === today });
    }
    return cells;
  });
}
