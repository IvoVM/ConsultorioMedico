import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentStatus, AppointmentsService, errorMessage } from 'sdk';
import { FechaPipe, UiBadge, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';

const statusLabels: Record<AppointmentStatus, string> = {
  [AppointmentStatus.Booked]: 'Reservado',
  [AppointmentStatus.CheckedIn]: 'Admitido',
  [AppointmentStatus.InProgress]: 'En atención',
  [AppointmentStatus.Completed]: 'Atendido',
  [AppointmentStatus.Cancelled]: 'Cancelado',
  [AppointmentStatus.NoShow]: 'Ausente',
};

@Component({
  imports: [FormsModule, RouterLink, FechaPipe, SortCol, TablePager, UiBadge, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './history.html',
})
export class AppointmentHistoryPage {
  private readonly appointments = inject(AppointmentsService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly statusLabels = statusLabels;
  readonly status = AppointmentStatus;
  readonly rows = signal<AppointmentDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'start', dir: 'desc' });

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
      const result = await firstValueFrom(this.appointments.appointmentsPage(listParams(this.page(), this.sort(), this.query())));
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
