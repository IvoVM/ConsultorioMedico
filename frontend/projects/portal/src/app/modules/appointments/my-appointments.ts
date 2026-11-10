import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentDto, AppointmentStatus, AppointmentsService, BookingRequestDto, BookingRequestStatus, OrganizationService, errorMessage } from 'sdk';
import { appointmentStatusLabels } from './models/appointment-status-labels';
import { FechaPipe, SelectOption, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable, UiTabs, TabItem, UiTone, siteLabel } from 'ui';
import { MyAppointmentsOrder, MyAppointmentsTab, matchesSearch, myAppointmentsTabLabels, myAppointmentsOrderLabels, sortMine } from './models/my-appointments-order';
import { firstValueFrom } from 'rxjs';
import { AppRoute } from '../../app.routes.enum';
import { AppointmentChanges } from './appointment-changes';

@Component({
  imports: [FormsModule, RouterLink, AppointmentChanges, FechaPipe, UiTable, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTabs],
  templateUrl: './my-appointments.html',
})
export class MyAppointmentsPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly organization = inject(OrganizationService);
  // With more than one active site each request names where the visit is; otherwise it reads "Sede única".
  readonly multiSite = signal(false);
  readonly statusLabels = appointmentStatusLabels;
  readonly status = AppointmentStatus;
  readonly route = AppRoute;
  readonly booking = BookingRequestStatus;
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly reasonFor = signal<BookingRequestDto | null>(null);
  readonly trailOf = signal<AppointmentDto | null>(null);
  readonly tabKeys = MyAppointmentsTab;
  readonly tab = signal<string>(MyAppointmentsTab.Turnos);
  readonly tabs = computed((): TabItem[] => [
    { key: MyAppointmentsTab.Turnos, label: myAppointmentsTabLabels[MyAppointmentsTab.Turnos], icon: 'event_available', count: this.appointments().length },
    {
      key: MyAppointmentsTab.Solicitudes,
      label: myAppointmentsTabLabels[MyAppointmentsTab.Solicitudes],
      icon: 'pending_actions',
      count: this.requests().filter((item) => item.status === BookingRequestStatus.Pending).length,
      tone: UiTone.Warn,
    },
  ]);
  readonly order = signal<string>(MyAppointmentsOrder.Creado);
  readonly search = signal('');
  readonly orderOptions: SelectOption[] = (Object.values(MyAppointmentsOrder) as MyAppointmentsOrder[]).map((value) => ({ value, label: myAppointmentsOrderLabels[value] }));
  readonly shownRequests = computed(() =>
    sortMine(this.requests().filter((item) => matchesSearch(this.search(), item.specialty, item.professional, item.appointmentType, item.location)), this.order() as MyAppointmentsOrder),
  );
  readonly shownAppointments = computed(() =>
    sortMine(this.appointments().filter((item) => matchesSearch(this.search(), item.specialty, item.professional, item.appointmentType, item.location)), this.order() as MyAppointmentsOrder),
  );
  readonly hasReason = computed(() => this.shownRequests().some((request) => request.status === BookingRequestStatus.Rejected));
  readonly requestStatus: Record<BookingRequestStatus, string> = {
    [BookingRequestStatus.Pending]: 'Pendiente',
    [BookingRequestStatus.Approved]: 'Confirmada',
    [BookingRequestStatus.Rejected]: 'Rechazada',
    [BookingRequestStatus.Rescheduled]: 'Reprogramada',
  };
  readonly error = signal('');
  readonly loading = signal(true);
  readonly pending = signal('');

  constructor() {
    void this.load();
    void this.loadSites();
  }

  site(location: string) {
    return siteLabel(location, this.multiSite());
  }

  private async loadSites() {
    try {
      const sites = await firstValueFrom(this.organization.locations());
      this.multiSite.set(sites.filter((item) => item.isActive).length > 1);
    } catch {
      this.multiSite.set(false);
    }
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

  tone(status: BookingRequestStatus) {
    if (status === BookingRequestStatus.Rejected) return 'warn' as const;
    if (status === BookingRequestStatus.Rescheduled) return 'muted' as const;
    return 'ok' as const;
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
