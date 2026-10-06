import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentsService, clinicaSession, errorMessage } from 'sdk';
import { UiBadge, UiEmpty, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';

@Component({
  imports: [RouterLink, UiBadge, UiEmpty, UiSkeleton, UiTable],
  templateUrl: './dashboard.html',
})
export class DashboardPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  readonly session = clinicaSession;
  readonly statusLabels = appointmentStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly dayLabel = new Intl.DateTimeFormat('es-AR', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date());

  constructor() {
    void this.load();
  }

  timeLabel(start: string) {
    return start.slice(11, 16);
  }

  whenLabel(start: string) {
    const date = new Date(start);
    if (Number.isNaN(date.getTime())) return start;
    return new Intl.DateTimeFormat('es-AR', {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  }

  private today() {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }

  private async load() {
    const role = this.session.role();
    try {
      if (role === 'Patient') {
        const all = await firstValueFrom(this.appointmentsApi.myAppointments());
        const start = new Date();
        start.setHours(0, 0, 0, 0);
        this.appointments.set(
          all
            .filter((item) => item.status !== 'Cancelled' && item.status !== 'NoShow' && new Date(item.start) >= start)
            .sort((a, b) => a.start.localeCompare(b.start))
            .slice(0, 5),
        );
      } else if (role === 'Doctor' || role === 'Secretary' || role === 'TenantAdmin') {
        const list = await firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: this.today() }));
        this.appointments.set([...list].sort((a, b) => a.start.localeCompare(b.start)));
      }
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
