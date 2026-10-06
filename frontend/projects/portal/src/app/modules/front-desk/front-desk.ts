import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentDto, AppointmentsService, BillingService, InvoiceDto, PatientLookupDto, PatientsService, PaymentMethod, WaitlistEntryDto, errorMessage } from 'sdk';
import { FechaPipe, UiBadge, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { invoiceStatusLabels } from './models/invoice-labels';

@Component({
  imports: [FormsModule, FechaPipe, UiBadge, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './front-desk.html',
})
export class FrontDeskPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly billing = inject(BillingService);
  private readonly patientsApi = inject(PatientsService);
  private searchSeq = 0;
  private searchTimer = 0;
  readonly statusLabels = appointmentStatusLabels;
  readonly invoiceStatusLabels = invoiceStatusLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly waitlist = signal<WaitlistEntryDto[]>([]);
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly pending = signal('');
  readonly searchError = signal('');
  readonly searching = signal(false);
  readonly matches = signal<PatientLookupDto[]>([]);
  query = '';
  date = new Date().toISOString().slice(0, 10);
  appointmentId = '';
  newStart = '';
  waitlistEntryId = '';
  appointmentTypeId = '';

  constructor() {
    void this.load();
  }

  onQuery(value: string) {
    this.query = value;
    window.clearTimeout(this.searchTimer);
    this.searchTimer = window.setTimeout(() => void this.searchPatients(), 250);
  }

  age(birthDate: string) {
    const birth = new Date(`${birthDate}T00:00:00`);
    if (Number.isNaN(birth.getTime())) return null;
    const today = new Date();
    let years = today.getFullYear() - birth.getFullYear();
    const months = today.getMonth() - birth.getMonth();
    if (months < 0 || (months === 0 && today.getDate() < birth.getDate())) years--;
    return years;
  }

  private async searchPatients() {
    const q = this.query.trim();
    const seq = ++this.searchSeq;
    if (q.length < 2) {
      this.matches.set([]);
      this.searchError.set('');
      this.searching.set(false);
      return;
    }
    this.searching.set(true);
    this.searchError.set('');
    try {
      const matches = await firstValueFrom(this.patientsApi.searchPatients({ q }));
      if (seq !== this.searchSeq) return;
      this.matches.set(matches);
    } catch (error) {
      if (seq !== this.searchSeq) return;
      this.matches.set([]);
      this.searchError.set(errorMessage(error));
    } finally {
      if (seq === this.searchSeq) this.searching.set(false);
    }
  }

  async load() {
    if (!this.loading()) this.refreshing.set(true);
    this.error.set('');
    try {
      await this.fetchDay();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
      this.refreshing.set(false);
    }
  }

  async checkIn(appointment: AppointmentDto) {
    await this.run(`checkin:${appointment.id}`, () => this.appointmentsApi.checkInAppointment(appointment.id));
  }

  async cancel(appointment: AppointmentDto) {
    await this.run(`cancel:${appointment.id}`, () => this.appointmentsApi.cancelAppointment(appointment.id));
  }

  async reschedule() {
    await this.run('reschedule', () => this.appointmentsApi.rescheduleAppointment(this.appointmentId, { start: new Date(this.newStart).toISOString() }));
  }

  pickWaitlistEntry(item: WaitlistEntryDto) {
    this.waitlistEntryId = item.id;
  }

  async assign() {
    await this.run('assign', () =>
      this.appointmentsApi.assignWaitlistEntry(this.waitlistEntryId, {
        start: new Date(this.newStart).toISOString(),
        appointmentTypeId: this.appointmentTypeId,
      }),
    );
  }

  async pay(item: InvoiceDto, method: PaymentMethod) {
    await this.run(`pay:${item.id}:${method}`, () => this.billing.payInvoice(item.id, { method }));
  }

  private async fetchDay() {
    const [appointments, waitlist, invoices] = await Promise.all([
      firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: this.date })),
      firstValueFrom(this.appointmentsApi.waitlist()),
      firstValueFrom(this.billing.invoices()),
    ]);
    this.appointments.set(appointments);
    this.waitlist.set(waitlist);
    this.invoices.set(invoices);
  }

  private async run(key: string, call: () => Observable<unknown>) {
    this.pending.set(key);
    this.error.set('');
    try {
      await firstValueFrom(call());
      await this.fetchDay();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }
}
