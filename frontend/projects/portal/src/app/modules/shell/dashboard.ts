import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentStatus, AppointmentsService, BookingRequestDto, TenantRole, clinicaSession, errorMessage } from 'sdk';
import { BoardColumn, MediaWatcherService, ToastService, UiBadge, UiBoard, UiButton, UiEmpty, UiIcon, UiModal, UiSkeleton, UiStat, UiTable, UiTone, formatFecha, greeting, todayLabel } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { appointmentStatusTones } from '../appointments/models/appointment-status-tones';
import { AppRoute } from '../../app.routes.enum';

const flow = [AppointmentStatus.Booked, AppointmentStatus.CheckedIn, AppointmentStatus.InProgress, AppointmentStatus.Completed].map((status) => ({
  status,
  tone: appointmentStatusTones[status],
}));

@Component({
  imports: [RouterLink, UiBadge, UiBoard, UiButton, UiEmpty, UiIcon, UiModal, UiSkeleton, UiStat, UiTable],
  templateUrl: './dashboard.html',
})
export class DashboardPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly media = inject(MediaWatcherService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  readonly session = clinicaSession;
  readonly roles = TenantRole;
  readonly status = AppointmentStatus;
  readonly route = AppRoute;
  readonly statusLabels = appointmentStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly tone = UiTone;
  readonly dayLabel = todayLabel();
  readonly hello = greeting(this.session.name());
  readonly desk = computed(() => this.session.role() === TenantRole.Secretary || this.session.role() === TenantRole.TenantAdmin);
  readonly picked = signal<AppointmentDto | null>(null);
  readonly confirmingCancel = signal(false);
  readonly saving = signal(false);
  readonly actionError = signal('');
  readonly canChange = computed(() => {
    const status = this.picked()?.status;
    return status === AppointmentStatus.Booked || status === AppointmentStatus.Rescheduled;
  });

  readonly count = (status: AppointmentStatus) => this.appointments().filter((item) => item.status === status).length;
  readonly missed = computed(() => this.count(AppointmentStatus.NoShow) + this.count(AppointmentStatus.Cancelled));
  readonly next = computed(() => this.appointments().find((item) => item.status === AppointmentStatus.Booked || item.status === AppointmentStatus.CheckedIn) ?? null);
  readonly columns = computed((): BoardColumn[] => {
    const doctor = this.session.role() === TenantRole.Doctor;
    return flow.map(({ status, tone }) => ({
      key: status,
      label: this.statusLabels[status],
      tone,
      items: this.appointments()
        .filter((item) => item.status === status)
        .map((item) => ({
          id: item.id,
          title: item.patient,
          time: this.timeLabel(item.start),
          meta: doctor ? item.visitReason || 'Consulta' : item.professional,
          link: doctor ? AppRoute.Historia : undefined,
          queryParams: doctor ? { paciente: item.patientId } : undefined,
          linkLabel: 'Historia',
        })),
    }));
  });

  constructor() {
    void this.load();
    // Front desk and doctors change appointments all day; refresh the board quietly while the tab is visible.
    const timer = setInterval(() => {
      if (!document.hidden && !this.picked() && !this.saving() && this.session.role() !== TenantRole.Patient) void this.load(true);
    }, 30000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  openChart(patientId: string, event: Event) {
    if (!this.media.mobile()) return;
    const target = event.target;
    if (target instanceof Element && target.closest('a, button, ui-button')) return;
    void this.router.navigate([AppRoute.Historia], { queryParams: { paciente: patientId } });
  }

  pick(id: string) {
    this.picked.set(this.appointments().find((item) => item.id === id) ?? null);
    this.confirmingCancel.set(false);
    this.actionError.set('');
  }

  closePick() {
    if (this.saving()) return;
    this.picked.set(null);
  }

  checkIn() {
    const item = this.picked();
    if (!item) return;
    void this.change(this.appointmentsApi.checkInAppointment(item.id), 'En sala de espera', `${item.patient} está en sala de espera.`);
  }

  cancel() {
    const item = this.picked();
    if (!item) return;
    void this.change(this.appointmentsApi.cancelAppointment(item.id), 'Turno cancelado', `El horario de las ${this.timeLabel(item.start)} quedó libre.`);
  }

  timeLabel(start: string) {
    return start.slice(11, 16);
  }

  whenLabel(start: string) {
    return formatFecha(start);
  }

  private today() {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }

  private async change(request: Observable<AppointmentDto>, title: string, message: string) {
    this.saving.set(true);
    this.actionError.set('');
    try {
      const updated = await firstValueFrom(request);
      this.appointments.update((list) => list.map((item) => (item.id === updated.id ? updated : item)));
      this.toasts.success(title, message);
      this.picked.set(null);
    } catch (error) {
      this.actionError.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  private async load(quiet = false) {
    const role = this.session.role();
    try {
      if (role === TenantRole.Patient) {
        const all = await firstValueFrom(this.appointmentsApi.myAppointments());
        const start = new Date();
        start.setHours(0, 0, 0, 0);
        this.appointments.set(
          all
            .filter((item) => item.status !== AppointmentStatus.Cancelled && item.status !== AppointmentStatus.NoShow && new Date(item.start) >= start)
            .sort((a, b) => a.start.localeCompare(b.start))
            .slice(0, 5),
        );
      } else if (role === TenantRole.Doctor || role === TenantRole.Secretary || role === TenantRole.TenantAdmin) {
        const list = await firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: this.today() }));
        this.appointments.set([...list].sort((a, b) => a.start.localeCompare(b.start)));
        if (role === TenantRole.Secretary || role === TenantRole.TenantAdmin) {
          this.requests.set(await firstValueFrom(this.appointmentsApi.pendingRequests()));
        }
      }
    } catch (error) {
      if (!quiet) this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
