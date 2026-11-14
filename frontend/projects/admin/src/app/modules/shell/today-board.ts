import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentStatus, AppointmentsService, TenantRole, clinicaSession, errorMessage } from 'sdk';
import { BoardColumn, UiTone, MediaWatcherService, UiBadge, UiBoard, UiEmpty, UiIcon, UiSkeleton, UiStat, UiTable, greeting, todayLabel, UiHelp } from 'ui';
import { firstValueFrom } from 'rxjs';
import { appointmentStatusLabels as statusLabels, appointmentStatusTone } from '../appointments/models/appointment-status-labels';

const flow: { status: AppointmentStatus; tone: UiTone }[] = [
  { status: AppointmentStatus.Booked, tone: UiTone.Muted },
  { status: AppointmentStatus.CheckedIn, tone: UiTone.Warn },
  { status: AppointmentStatus.InProgress, tone: UiTone.Sky },
  { status: AppointmentStatus.Completed, tone: UiTone.Ok },
];

@Component({
  imports: [UiHelp, RouterLink, UiBadge, UiBoard, UiEmpty, UiIcon, UiSkeleton, UiStat, UiTable],
  selector: 'today-board',
  templateUrl: './today-board.html',
})
export class TodayBoard {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly media = inject(MediaWatcherService);
  private readonly router = inject(Router);
  readonly statusLabels = statusLabels;
  readonly statusTone = appointmentStatusTone;
  readonly status = AppointmentStatus;
  readonly tone = UiTone;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly dayLabel = todayLabel();
  readonly hello = greeting(clinicaSession.name());
  // Patient files, records and schedules are admin-only screens, so front desk does not get links to them.
  readonly isAdmin = clinicaSession.role() === TenantRole.TenantAdmin;

  readonly count = (status: AppointmentStatus) => this.appointments().filter((item) => item.status === status).length;
  readonly missed = computed(() => this.count(AppointmentStatus.NoShow) + this.count(AppointmentStatus.Cancelled));
  readonly columns = computed((): BoardColumn[] =>
    flow.map(({ status, tone }) => ({
      key: status,
      label: statusLabels[status],
      tone,
      items: this.appointments()
        .filter((item) => item.status === status)
        .map((item) => ({
          id: item.id,
          title: item.patient,
          time: this.timeLabel(item.start),
          meta: item.professional,
          ...(this.isAdmin ? { link: ['/pacientes', item.patientId], linkLabel: 'Ficha' } : {}),
        })),
    })),
  );

  constructor() {
    void this.load();
  }

  openFile(patientId: string, event: Event) {
    if (!this.isAdmin || !this.media.mobile()) return;
    const target = event.target;
    if (target instanceof Element && target.closest('a, button, ui-button')) return;
    void this.router.navigate(['/pacientes', patientId]);
  }

  timeLabel(start: string) {
    return start.slice(11, 16);
  }

  private async load() {
    const now = new Date();
    const date = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
    try {
      const list = await firstValueFrom(this.appointmentsApi.appointmentsForDay({ date }));
      this.appointments.set([...list].sort((a, b) => a.start.localeCompare(b.start)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
