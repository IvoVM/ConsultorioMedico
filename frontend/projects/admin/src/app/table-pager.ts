import { Component, computed, input, output } from '@angular/core';
import { ListParams } from 'sdk';
import { SortState } from './table-sort';

@Component({
  selector: 'table-pager',
  template: `
    <nav class="table-pager" aria-label="Páginas">
      <span>{{ from() }}–{{ to() }} de {{ total() }}</span>
      <button type="button" (click)="pageChange.emit(page() - 1)" [disabled]="page() <= 1">Anterior</button>
      <span>{{ page() }} / {{ pages() }}</span>
      <button type="button" (click)="pageChange.emit(page() + 1)" [disabled]="page() >= pages()">Siguiente</button>
    </nav>
  `,
})
export class TablePager {
  readonly total = input(0);
  readonly page = input(1);
  readonly pageSize = input(10);
  readonly pageChange = output<number>();
  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize()) || 1));
  readonly from = computed(() => (this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  readonly to = computed(() => Math.min(this.total(), this.page() * this.pageSize()));
}

export function listParams(page: number, sort: SortState, q: string, filter?: string): ListParams {
  return {
    page,
    sort: sort.key,
    dir: sort.dir,
    q: q.trim() || undefined,
    filter: filter && filter !== 'all' ? filter : undefined,
  };
}

export function queueSearch(host: { timer?: ReturnType<typeof setTimeout> }, run: () => void) {
  clearTimeout(host.timer);
  host.timer = setTimeout(run, 250);
}
