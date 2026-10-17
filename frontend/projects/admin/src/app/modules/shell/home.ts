import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentsService, errorMessage } from 'sdk';
import { UiBadge, UiEmpty, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

const statusLabels: Record<string, string> = {
  Booked: 'Reservado',
  CheckedIn: 'Admitido',
  InProgress: 'En atención',
  Completed: 'Atendido',
  Cancelled: 'Cancelado',
  NoShow: 'Ausente',
};

@Component({
  imports: [RouterLink, UiBadge, UiEmpty, UiSkeleton, UiTable],
  templateUrl: './home.html',
})
export class HomePage {
  private readonly appointmentsApi = inject(AppointmentsService);
  readonly statusLabels = statusLabels;
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
