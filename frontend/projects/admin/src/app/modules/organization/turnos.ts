import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AppointmentKind, AppointmentTypeRowDto, OrganizationService, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, ScreenNote, SortCol, TablePager, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './turnos.html',
})
export class TurnosPage {
  private readonly organization = inject(OrganizationService);
  private readonly router = inject(Router);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly rows = signal<AppointmentTypeRowDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly creating = signal(false);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  readonly kindOptions = [
    { value: AppointmentKind.Consulta, label: 'Consulta médica' },
    { value: AppointmentKind.Estudio, label: 'Estudio' },
  ];
  typeName = '';
  duration = 30;
  kind = AppointmentKind.Consulta;

  constructor() {
    void this.load();
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

  async createTurno() {
    const name = this.typeName.trim();
    if (!name) return;
    this.error.set('');
    this.saving.set(true);
    try {
      const created = await firstValueFrom(
        this.organization.createAppointmentType({ name, durationMinutes: Number(this.duration || 30), specialtyId: null, kind: this.kind }),
      );
      await this.router.navigate(['/organizacion/turnos', created.id]);
    } catch (error) {
      this.error.set(errorMessage(error));
      this.saving.set(false);
    }
  }

  private async load() {
    try {
      const result = await firstValueFrom(
        this.organization.appointmentTypesPage(listParams(this.page(), this.sort(), this.query())),
      );
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
