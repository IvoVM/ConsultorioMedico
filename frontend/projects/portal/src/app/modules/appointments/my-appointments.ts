import { Component, inject, signal } from '@angular/core';
import { AppointmentDto, AppointmentsService, BookingRequestDto, errorMessage } from 'sdk';
import { appointmentStatusLabels } from './models/appointment-status-labels';
import { FechaPipe, UiBadge, UiButton, UiEmpty, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FechaPipe, UiTable, UiBadge, UiButton, UiEmpty, UiSkeleton],
  templateUrl: './my-appointments.html',
})
export class MyAppointmentsPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  readonly statusLabels = appointmentStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly requestStatus: Record<string, string> = { Pending: 'Pendiente', Approved: 'Confirmada', Rejected: 'Rechazada' };
  readonly error = signal('');
  readonly loading = signal(true);
  readonly pending = signal('');

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const [appointments, requests] = await Promise.all([
        firstValueFrom(this.appointmentsApi.myAppointments()),
        firstValueFrom(this.appointmentsApi.myRequests()),
      ]);
      this.appointments.set(appointments);
      this.requests.set(requests);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async cancel(appointment: AppointmentDto) {
    this.pending.set(appointment.id);
    try {
      await firstValueFrom(this.appointmentsApi.cancelAppointment(appointment.id));
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }
}
