import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentsService, BillingService, BookingRequestDto, InvoiceDto, PatientLookupDto, PatientsService, PaymentMethod, ScheduleService, SlotDto, WaitlistEntryDto, errorMessage } from 'sdk';
import { FechaPipe, UiActions, UiBadge, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable, formatFecha } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { invoiceStatusLabels } from './models/invoice-labels';

const paymentLabels: Record<PaymentMethod, string> = {
  Cash: 'efectivo',
  Transfer: 'transferencia',
  Card: 'tarjeta',
};

@Component({
  imports: [FormsModule, RouterLink, FechaPipe, UiActions, UiBadge, UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './front-desk.html',
})
export class FrontDeskPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly billing = inject(BillingService);
  private readonly patientsApi = inject(PatientsService);
  private readonly schedule = inject(ScheduleService);
  private searchSeq = 0;
  private searchTimer = 0;
  readonly statusLabels = appointmentStatusLabels;
  readonly invoiceStatusLabels = invoiceStatusLabels;
  readonly paymentLabels = paymentLabels;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly waitlist = signal<WaitlistEntryDto[]>([]);
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly moving = signal<AppointmentDto | null>(null);
  readonly error = signal('');
  readonly notice = signal('');
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly pending = signal('');
  readonly slotsLoading = signal(false);
  readonly searchError = signal('');
  readonly searching = signal(false);
  readonly matches = signal<PatientLookupDto[]>([]);
  query = '';
  date = new Date().toISOString().slice(0, 10);
  moveDate = this.date;

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
    if (Number.isNaN(birth.getTime()) || birth.getFullYear() < 1901) return null;
    const today = new Date();
    let years = today.getFullYear() - birth.getFullYear();
    const months = today.getMonth() - birth.getMonth();
    if (months < 0 || (months === 0 && today.getDate() < birth.getDate())) years--;
    return years;
  }

  charge(appointment: AppointmentDto) {
    return this.invoices().find((item) => item.appointmentId === appointment.id) ?? null;
  }

  money(amount: number) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(amount);
  }

  canMove(appointment: AppointmentDto) {
    return appointment.status === 'Booked' || appointment.status === 'CheckedIn';
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

  coverageLabel(appointment: AppointmentDto) {
    if (!appointment.insurer) return 'Particular';
    const number = appointment.memberNumber ? ` · afiliado ${appointment.memberNumber}` : '';
    if (appointment.coverageAccepted === true) return `${appointment.insurer}${number} · número correcto`;
    if (appointment.coverageAccepted === false) return `${appointment.insurer}${number} · no coincide`;
    return `${appointment.insurer}${number} · pendiente de revisión`;
  }

  async review(appointment: AppointmentDto, accepted: boolean) {
    await this.run(`coverage:${appointment.id}:${accepted}`, () => this.appointmentsApi.reviewCoverage(appointment.id, accepted));
  }

  async checkIn(appointment: AppointmentDto) {
    await this.run(`checkin:${appointment.id}`, () => this.appointmentsApi.checkInAppointment(appointment.id));
  }

  async absent(appointment: AppointmentDto) {
    await this.run(`absent:${appointment.id}`, () => this.appointmentsApi.markNoShow(appointment.id));
  }

  async cancel(appointment: AppointmentDto) {
    await this.run(`cancel:${appointment.id}`, () => this.appointmentsApi.cancelAppointment(appointment.id));
  }

  async startMove(appointment: AppointmentDto) {
    this.moving.set(appointment);
    this.moveDate = appointment.start.slice(0, 10);
    this.slots.set([]);
    await this.loadSlots();
  }

  cancelMove() {
    this.moving.set(null);
    this.slots.set([]);
  }

  async loadSlots() {
    const appointment = this.moving();
    if (!appointment || !this.moveDate) return;
    this.slotsLoading.set(true);
    this.error.set('');
    try {
      const slots = await firstValueFrom(
        this.schedule.availability({
          locationId: appointment.locationId,
          specialtyId: '00000000-0000-0000-0000-000000000000',
          professionalId: appointment.professionalId,
          date: this.moveDate,
        }),
      );
      this.slots.set(slots.filter((slot) => slot.appointmentTypeId === appointment.appointmentTypeId));
    } catch (error) {
      this.slots.set([]);
      this.error.set(errorMessage(error));
    } finally {
      this.slotsLoading.set(false);
    }
  }

  async moveTo(slot: SlotDto) {
    const appointment = this.moving();
    if (!appointment) return;
    await this.run(`move:${appointment.id}`, () => this.appointmentsApi.rescheduleAppointment(appointment.id, { start: slot.start }));
    if (!this.error()) this.cancelMove();
  }

  async offer(item: WaitlistEntryDto) {
    this.pending.set(`offer:${item.id}`);
    this.error.set('');
    this.notice.set('');
    try {
      const appointment = await firstValueFrom(this.appointmentsApi.offerWaitlist(item.id));
      this.notice.set(`${item.patient} tiene turno el ${formatFecha(appointment.start)} con ${appointment.professional}.`);
      await this.fetchDay();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  async decide(item: BookingRequestDto, accepted: boolean) {
    this.pending.set(`${accepted ? 'approve' : 'reject'}:${item.id}`);
    this.error.set('');
    this.notice.set('');
    try {
      if (accepted) {
        await firstValueFrom(this.appointmentsApi.approveRequest(item.id));
        this.notice.set(`${item.name} quedó el ${formatFecha(item.start)} con ${item.professional}.`);
      } else {
        await firstValueFrom(this.appointmentsApi.rejectRequest(item.id));
        this.notice.set(`La solicitud de ${item.name} quedó rechazada.`);
      }
      await this.fetchDay();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  async pay(item: InvoiceDto, method: PaymentMethod) {
    await this.run(`pay:${item.id}:${method}`, () => this.billing.payInvoice(item.id, { method }));
  }

  private async fetchDay() {
      const [appointments, waitlist, invoices, requests] = await Promise.all([
        firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: this.date })),
        firstValueFrom(this.appointmentsApi.waitlist()),
        firstValueFrom(this.billing.invoices()),
        firstValueFrom(this.appointmentsApi.pendingRequests()),
      ]);
      this.appointments.set(appointments);
      this.waitlist.set(waitlist);
      this.invoices.set(invoices);
      this.requests.set(requests);
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
