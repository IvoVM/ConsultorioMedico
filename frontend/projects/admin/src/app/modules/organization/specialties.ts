import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OrganizationService, SpecialtyRowDto, errorMessage } from 'sdk';
import { MediaWatcherService, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, ScreenNote, SortCol, TablePager, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './specialties.html',
})
export class SpecialtiesPage {
  private readonly organization = inject(OrganizationService);
  private readonly router = inject(Router);
  private readonly media = inject(MediaWatcherService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly rows = signal<SpecialtyRowDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly creating = signal(false);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  specialtyName = '';

  constructor() {
    void this.load();
  }

  openFile(id: string, event: Event) {
    if (!this.media.mobile()) return;
    const target = event.target;
    if (target instanceof Element && target.closest('a, button, ui-button')) return;
    void this.router.navigate(['/organizacion/especialidades', id]);
  }

  countLabel(count: number, one: string, many: string) {
    return count === 1 ? `1 ${one}` : `${count} ${many}`;
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

  async createSpecialty() {
    this.error.set('');
    this.saving.set(true);
    try {
      const created = await firstValueFrom(this.organization.createSpecialty({ name: this.specialtyName.trim() }));
      await this.router.navigate(['/organizacion/especialidades', created.id]);
    } catch (error) {
      this.error.set(errorMessage(error));
      this.saving.set(false);
    }
  }

  private async load() {
    try {
      const result = await firstValueFrom(this.organization.specialtiesPage(listParams(this.page(), this.sort(), this.query())));
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
