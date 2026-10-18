import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InsurerDto, OrganizationService, errorMessage } from 'sdk';
import { UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, ScreenNote, SortCol, TablePager, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './insurers.html',
})
export class InsurersPage {
  private readonly organization = inject(OrganizationService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly rows = signal<InsurerDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly creating = signal(false);
  readonly pending = signal('');
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  name = '';

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

  async create() {
    this.error.set('');
    this.saving.set(true);
    try {
      await firstValueFrom(this.organization.createInsurer(this.name.trim()));
      this.name = '';
      this.creating.set(false);
      this.page.set(1);
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  async toggle(item: InsurerDto) {
    this.error.set('');
    this.pending.set(item.id);
    try {
      await firstValueFrom(this.organization.setInsurerEnabled(item.id, !item.enabled));
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  private async load() {
    try {
      const result = await firstValueFrom(this.organization.insurersPage(listParams(this.page(), this.sort(), this.query())));
      this.rows.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
