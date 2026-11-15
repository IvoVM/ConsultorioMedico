import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentStatus, AppointmentsService, BillingService, ClinicService, InvoiceDto, InvoiceStatus, PaymentMethod, errorMessage } from 'sdk';
import { FechaPipe, UiButton, UiEmpty, UiHelp, UiIcon, UiInvoice, UiSkeleton, formatMoney, localDate } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { invoiceStatusLabels } from '../billing/models/invoice-labels';
import { cardBrandOptions, invoiceSource, invoiceView, paymentOptions } from '../billing/models/invoice-view';
import { AppRoute, appointmentParams } from '../../app.routes.enum';
import { DeskStage, deskStageLabels } from './models/desk-stage';

// The front desk's day, by where each patient is: arriving (check the coverage, send them to the waiting
// room or mark them absent), inside (waiting or being seen) and done (issue and charge the invoice).
// Moving or cancelling a booking is done on the agenda, the one place that knows rooms, equipment and overbooks.
@Component({
  imports: [RouterLink, FechaPipe, UiButton, UiEmpty, UiHelp, UiIcon, UiInvoice, UiSkeleton],
  templateUrl: './front-desk.html',
  styleUrl: './front-desk.css',
})
export class FrontDeskPage {
  readonly route = AppRoute;
  readonly appointmentParams = appointmentParams;
  readonly status = AppointmentStatus;
  readonly invoice = InvoiceStatus;
  readonly stages = DeskStage;
  readonly stageLabels = deskStageLabels;
  readonly stageOrder = [DeskStage.Arriving, DeskStage.Inside, DeskStage.Checkout];
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly billing = inject(BillingService);
  private readonly clinic = inject(ClinicService);
  readonly statusLabels = appointmentStatusLabels;
  readonly invoiceStatusLabels = invoiceStatusLabels;
  readonly paymentOptions = paymentOptions;
  readonly brandOptions = cardBrandOptions;
  readonly cardMethod = PaymentMethod.Card;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly invoices = signal<InvoiceDto[]>([]);
  // With automatic invoicing off, front desk issues the invoice of an attended appointment by hand.
  readonly billingEnabled = signal(false);
  readonly fiscalEnabled = signal(false);
  // The invoice open in the detail dialog, where front desk charges, corrects, authorizes or voids it.
  readonly opened = signal<InvoiceDto | null>(null);
  readonly openedView = computed(() => {
    const invoice = this.opened();
    return invoice ? invoiceView(invoice, { staff: true, fiscalEnabled: this.fiscalEnabled() }) : null;
  });
  readonly openedSource = computed(() => {
    const invoice = this.opened();
    return invoice
      ? invoiceSource(this.billing, invoice, async (updated) => {
          this.opened.set(updated);
          await this.fetchDay();
        })
      : null;
  });
  readonly error = signal('');
  readonly loading = signal(true);
  readonly pending = signal('');

  readonly board = computed(() => {
    const columns: Record<DeskStage, AppointmentDto[]> = { [DeskStage.Arriving]: [], [DeskStage.Inside]: [], [DeskStage.Checkout]: [] };
    for (const appointment of this.appointments()) {
      const stage = this.stageOf(appointment);
      if (stage) columns[stage].push(appointment);
    }
    return columns;
  });
  // What no longer needs the desk today: paid or free visits, and the ones that did not happen.
  readonly done = computed(() => this.appointments().filter((item) => item.status === AppointmentStatus.Completed && !this.stageOf(item)).length);
  readonly missed = computed(() => this.appointments().filter((item) => item.status === AppointmentStatus.NoShow || item.status === AppointmentStatus.Cancelled).length);

  constructor() {
    void this.load();
    void firstValueFrom(this.clinic.clinicProfile())
      .then((profile) => {
        this.billingEnabled.set(profile.billingEnabled);
        this.fiscalEnabled.set(profile.fiscalEnabled);
      })
      .catch(() => undefined);
    // Doctors and other desks change the day too: refresh quietly while nobody is mid-action here.
    const timer = setInterval(() => {
      if (!document.hidden && !this.pending() && !this.opened()) void this.fetchDay().catch(() => undefined);
    }, 30000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  private stageOf(appointment: AppointmentDto): DeskStage | null {
    switch (appointment.status) {
      case AppointmentStatus.Booked:
      case AppointmentStatus.Rescheduled:
        return DeskStage.Arriving;
      case AppointmentStatus.CheckedIn:
      case AppointmentStatus.InProgress:
        return DeskStage.Inside;
      case AppointmentStatus.Completed: {
        const bill = this.charge(appointment);
        if (bill) return bill.status === InvoiceStatus.Pending || bill.status === InvoiceStatus.PartiallyPaid ? DeskStage.Checkout : null;
        return this.billingEnabled() ? DeskStage.Checkout : null;
      }
      default:
        return null;
    }
  }

  charge(appointment: AppointmentDto) {
    return this.invoices().find((item) => item.appointmentId === appointment.id) ?? null;
  }

  money(amount: number) {
    return formatMoney(amount);
  }

  coverageLabel(appointment: AppointmentDto) {
    if (!appointment.insurer) return 'Particular';
    const number = appointment.memberNumber ? ` · afiliado ${appointment.memberNumber}` : '';
    if (appointment.coverageAccepted === true) return `${appointment.insurer}${number} · número correcto`;
    if (appointment.coverageAccepted === false) return `${appointment.insurer}${number} · no coincide`;
    return `${appointment.insurer}${number}`;
  }

  // The member number is checked against the card when the patient arrives.
  needsCoverage(appointment: AppointmentDto) {
    return !!appointment.insurer && appointment.coverageAccepted == null;
  }

  place(appointment: AppointmentDto) {
    return [appointment.professional, appointment.room].filter(Boolean).join(' · ');
  }

  async load() {
    this.error.set('');
    try {
      await this.fetchDay();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
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

  async issue(appointment: AppointmentDto) {
    this.pending.set(`issue:${appointment.id}`);
    this.error.set('');
    try {
      const invoice = await firstValueFrom(this.billing.issueInvoice({ appointmentId: appointment.id }));
      await this.fetchDay();
      this.opened.set(invoice);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  private async fetchDay() {
    const [appointments, invoices] = await Promise.all([
      firstValueFrom(this.appointmentsApi.appointmentsForDay({ date: localDate(new Date()) })),
      firstValueFrom(this.billing.invoices()),
    ]);
    this.appointments.set(appointments);
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
