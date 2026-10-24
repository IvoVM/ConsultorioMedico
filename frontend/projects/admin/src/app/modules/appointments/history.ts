import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentStatus, AppointmentsService, ScheduleService, SlotDto, errorMessage } from 'sdk';
import { FechaPipe, MediaWatcherService, UiBadge, UiButton, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
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
  [AppointmentStatus.Rescheduled]: 'Reprogramado',
};

@Component({
  imports: [FormsModule, RouterLink, FechaPipe, SortCol, TablePager, UiBadge, UiButton, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './history.html',
})
export class AppointmentHistoryPage {
  private readonly appointments = inject(AppointmentsService);
  private readonly schedule = inject(ScheduleService);
  private readonly media = inject(MediaWatcherService);
  private readonly router = inject(Router);
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
  readonly moving = signal<AppointmentDto | null>(null);
  readonly slots = signal<SlotDto[]>([]);
  readonly slotsLoading = signal(false);
  readonly pending = signal('');
  moveDate = new Date().toISOString().slice(0, 10);

  constructor() {
    void this.load();
  }

  openFile(patientId: string, event: Event) {
    if (!this.media.mobile()) return;
    const target = event.target;
    if (target instanceof Element && target.closest('a, button, ui-button')) return;
    void this.router.navigate(['/pacientes', patientId]);
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

  canMove(item: AppointmentDto) {
    return item.status === AppointmentStatus.Booked || item.status === AppointmentStatus.Rescheduled || item.status === AppointmentStatus.CheckedIn;
  }

  async startMove(item: AppointmentDto) {
    this.moving.set(item);
    this.moveDate = item.start.slice(0, 10);
    this.slots.set([]);
    await this.loadSlots();
  }

  cancelMove() {
    this.moving.set(null);
    this.slots.set([]);
  }

  async loadSlots() {
    const item = this.moving();
    if (!item || !this.moveDate) return;
    this.slotsLoading.set(true);
    this.error.set('');
    try {
      const slots = await firstValueFrom(this.schedule.availability({
        locationId: item.locationId,
        specialtyId: '00000000-0000-0000-0000-000000000000',
        professionalId: item.professionalId,
        date: this.moveDate,
      }));
      this.slots.set(slots.filter((slot) => slot.appointmentTypeId === item.appointmentTypeId));
    } catch (error) {
      this.slots.set([]);
      this.error.set(errorMessage(error));
    } finally {
      this.slotsLoading.set(false);
    }
  }

  async moveTo(slot: SlotDto) {
    const item = this.moving();
    if (!item) return;
    this.pending.set(item.id);
    this.error.set('');
    try {
      await firstValueFrom(this.appointments.rescheduleAppointment(item.id, { start: slot.start }));
      this.cancelMove();
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
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
