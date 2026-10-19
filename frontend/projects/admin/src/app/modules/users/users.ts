import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountUserDto, UsersService, errorMessage } from 'sdk';
import { UiActions, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { roleLabels } from '../employees/models/role-labels';

@Component({
  imports: [FormsModule, SortCol, TablePager, UiActions, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './users.html',
})
export class UsersPage {
  private readonly users = inject(UsersService);
  readonly roleLabels = roleLabels;
  readonly people = signal<AccountUserDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly target = signal<AccountUserDto | null>(null);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};

  constructor() {
    void this.load();
  }

  async load() {
    this.loading.set(true);
    try {
      const page = await firstValueFrom(this.users.usersPage(listParams(this.page(), this.sort(), this.query())));
      this.people.set(page.items);
      this.total.set(page.total);
      this.page.set(page.page);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
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

  ask(person: AccountUserDto) {
    this.error.set('');
    this.target.set(person);
  }

  async confirm() {
    const person = this.target();
    if (!person) return;
    this.error.set('');
    this.saving.set(true);
    try {
      await firstValueFrom(this.users.resetPassword(person.id));
      this.target.set(null);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
