import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AppointmentDto,
  ClinicaClient,
  InvoiceDto,
  PaymentMethod,
  WaitlistEntryDto,
  appointmentStatusLabels,
  errorMessage,
  invoiceStatusLabels,
} from 'sdk';
import { UiBadge, UiButton, UiCalendar, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiBadge, UiButton, UiCalendar, UiField, UiTable],
  templateUrl: './front-desk.html',
})
export class FrontDeskPage {
  private readonly api = inject(ClinicaClient);
  readonly statusLabels = appointmentStatusLabels;
  readonly invoiceStatusLabels = invoiceStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly waitlist = signal<WaitlistEntryDto[]>([]);
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly error = signal('');
  date = new Date().toISOString().slice(0, 10);
  appointmentId = '';
  newStart = '';
  waitlistEntryId = '';
  appointmentTypeId = '';

  constructor() {
    void this.load();
  }

  async load() {
    this.error.set('');
    try {
      const [appointments, waitlist, invoices] = await Promise.all([
        firstValueFrom(this.api.appointmentsForDay({ date: this.date })),
        firstValueFrom(this.api.waitlist()),
        firstValueFrom(this.api.invoices()),
      ]);
      this.appointments.set(appointments);
      this.waitlist.set(waitlist);
      this.invoices.set(invoices);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async checkIn(appointment: AppointmentDto) {
    await this.run(() => this.api.checkInAppointment(appointment.id));
  }

  async cancel(appointment: AppointmentDto) {
    await this.run(() => this.api.cancelAppointment(appointment.id));
  }

  async reschedule() {
    await this.run(() => this.api.rescheduleAppointment(this.appointmentId, { start: new Date(this.newStart).toISOString() }));
  }

  pickWaitlistEntry(item: WaitlistEntryDto) {
    this.waitlistEntryId = item.id;
  }

  async assign() {
    await this.run(() =>
      this.api.assignWaitlistEntry(this.waitlistEntryId, { start: new Date(this.newStart).toISOString(), appointmentTypeId: this.appointmentTypeId }),
    );
  }

  async pay(item: InvoiceDto, method: PaymentMethod) {
    await this.run(() => this.api.payInvoice(item.id, { method }));
  }

  private async run(call: () => Observable<unknown>) {
    this.error.set('');
    try {
      await firstValueFrom(call());
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
