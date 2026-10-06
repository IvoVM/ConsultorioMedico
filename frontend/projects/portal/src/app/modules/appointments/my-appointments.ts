import { Component, inject, signal } from '@angular/core';
import { AppointmentDto, AppointmentsService, errorMessage } from 'sdk';
import { appointmentStatusLabels } from './models/appointment-status-labels';
import { UiBadge, UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge, UiButton, UiEmpty],
  templateUrl: './my-appointments.html',
})
export class MyAppointmentsPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  readonly statusLabels = appointmentStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly error = signal('');

  constructor() {
    void this.load();
  }

  async load() {
    try {
      this.appointments.set(await firstValueFrom(this.appointmentsApi.myAppointments()));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async cancel(appointment: AppointmentDto) {
    try {
      await firstValueFrom(this.appointmentsApi.cancelAppointment(appointment.id));
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
