import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditEntryDto, AuditService, errorMessage } from 'sdk';
import { FechaPipe, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, FechaPipe, SortCol, TablePager, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './audit-log.html',
})
export class AuditLogPage {
  private readonly audit = inject(AuditService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly entries = signal<AuditEntryDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'date', dir: 'asc' });

  constructor() {
    void this.load();
  }

  sortBy(key: string) {
    this.sort.update((current) => nextSort(current, key));
    this.page.set(1);
    void this.load();
  }

  onSearch(value: string) {
    this.query.set(value);
    this.page.set(1);
    queueSearch(this.searchHost, () => void this.load());
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  private async load() {
    try {
      const result = await firstValueFrom(this.audit.auditPage(listParams(this.page(), this.sort(), this.query())));
      this.entries.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
