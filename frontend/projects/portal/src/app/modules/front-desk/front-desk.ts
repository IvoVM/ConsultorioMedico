import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentDto, AppointmentsService, BillingService, InvoiceDto, PaymentMethod, WaitlistEntryDto, errorMessage } from 'sdk';
import { UiBadge, UiButton, UiCalendar, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { invoiceStatusLabels } from './models/invoice-labels';

@Component({
  imports: [FormsModule, UiBadge, UiButton, UiCalendar, UiField, UiTable],
  templateUrl: './front-desk.html',
})
export class FrontDeskPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly billing = inject(BillingService);
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
        firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: this.date })),
        firstValueFrom(this.appointmentsApi.waitlist()),
        firstValueFrom(this.billing.invoices()),
      ]);
      this.appointments.set(appointments);
      this.waitlist.set(waitlist);
      this.invoices.set(invoices);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async checkIn(appointment: AppointmentDto) {
    await this.run(() => this.appointmentsApi.checkInAppointment(appointment.id));
  }

  async cancel(appointment: AppointmentDto) {
    await this.run(() => this.appointmentsApi.cancelAppointment(appointment.id));
  }

  async reschedule() {
    await this.run(() => this.appointmentsApi.rescheduleAppointment(this.appointmentId, { start: new Date(this.newStart).toISOString() }));
  }

  pickWaitlistEntry(item: WaitlistEntryDto) {
    this.waitlistEntryId = item.id;
  }

  async assign() {
    await this.run(() =>
      this.appointmentsApi.assignWaitlistEntry(this.waitlistEntryId, {
        start: new Date(this.newStart).toISOString(),
        appointmentTypeId: this.appointmentTypeId,
      }),
    );
  }

  async pay(item: InvoiceDto, method: PaymentMethod) {
    await this.run(() => this.billing.payInvoice(item.id, { method }));
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
