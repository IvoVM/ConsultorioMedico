import { Component, inject, signal } from '@angular/core';
import { AppointmentDto, ClinicaClient, appointmentStatusLabels, errorMessage } from 'sdk';
import { UiBadge, UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiBadge, UiButton, UiEmpty],
  templateUrl: './my-appointments.html',
})
export class MyAppointmentsPage {
  private readonly api = inject(ClinicaClient);
  readonly statusLabels = appointmentStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly error = signal('');

  constructor() {
    void this.load();
  }

  async load() {
    try {
      this.appointments.set(await firstValueFrom(this.api.myAppointments()));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async cancel(appointment: AppointmentDto) {
    try {
      await firstValueFrom(this.api.cancelAppointment(appointment.id));
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
