import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  AppointmentChangeDto,
  AppointmentDto,
  AppointmentHistoryFilter,
  AppointmentStatus,
  AppointmentsService,
  EmployeesService,
  OrganizationService,
  PatientLookupDto,
  PatientsService,
  ProfessionalDto,
  ScheduleService,
  SlotDto,
  SpecialtyDto,
  TenantRole,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { FechaPipe, MediaWatcherService, SelectOption, ToastService, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { appointmentStatusLabels, appointmentStatusTone } from './models/appointment-status-labels';

const anyOption: SelectOption = { value: '', label: 'Todos' };

@Component({
  imports: [FormsModule, RouterLink, FechaPipe, SortCol, TablePager, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './history.html',
})
export class AppointmentHistoryPage {
  private readonly appointments = inject(AppointmentsService);
  private readonly schedule = inject(ScheduleService);
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly patientsApi = inject(PatientsService);
  private readonly media = inject(MediaWatcherService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  private readonly patientHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly statusLabels = appointmentStatusLabels;
  readonly statusTone = appointmentStatusTone;
  // Patient files live in an admin-only screen; front desk gets the history without the link.
  readonly canOpenFile = clinicaSession.role() === TenantRole.TenantAdmin;
  readonly rows = signal<AppointmentDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'start', dir: 'desc' });

  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly patientMatches = signal<PatientLookupDto[]>([]);
  readonly specialtyId = signal('');
  readonly professionalId = signal('');
  readonly patientId = signal('');
  readonly patientText = signal('');
  readonly statusFilter = signal('');
  readonly past = signal(true);

  readonly specialtyOptions = computed<SelectOption[]>(() => [
    { value: '', label: 'Todas' },
    ...this.specialties().map((item) => ({ value: item.id, label: item.name })),
  ]);
  readonly professionalOptions = computed<SelectOption[]>(() => [
    anyOption,
    ...this.professionals()
      .filter((item) => !this.specialtyId() || item.specialtyId === this.specialtyId())
      .map((item) => ({ value: item.id, label: `${item.firstName} ${item.lastName}`.trim() })),
  ]);
  readonly statusOptions: SelectOption[] = [
    anyOption,
    ...(Object.keys(appointmentStatusLabels) as AppointmentStatus[]).map((status) => ({ value: status, label: appointmentStatusLabels[status] })),
  ];
  readonly filtered = computed(() => !!(this.query() || this.specialtyId() || this.professionalId() || this.patientId() || this.statusFilter()));

  readonly detail = signal<AppointmentDto | null>(null);
  readonly changes = signal<AppointmentChangeDto[]>([]);
  readonly changesLoading = signal(false);
  readonly changesError = signal('');

  readonly moving = signal<AppointmentDto | null>(null);
  readonly slots = signal<SlotDto[]>([]);
  readonly slotsError = signal('');
  readonly slotsLoading = signal(false);
  readonly pending = signal('');
  moveDate = new Date().toISOString().slice(0, 10);

  constructor() {
    void this.load();
    void this.loadFilters();
  }

  openFile(patientId: string, event: Event) {
    if (!this.canOpenFile || !this.media.mobile()) return;
    const target = event.target;
    if (target instanceof Element && target.closest('a, button, ui-button')) return;
    void this.router.navigate(['/pacientes', patientId]);
  }

  sortBy(key: string) {
    this.sort.update((current) => nextSort(current, key));
    this.reload();
  }

  onSearch(value: string) {
    this.query.set(value);
    this.page.set(1);
    queueSearch(this.searchHost, () => void this.load());
  }

  setSpecialty(value: string) {
    this.specialtyId.set(value);
    const professional = this.professionals().find((item) => item.id === this.professionalId());
    if (value && professional && professional.specialtyId !== value) this.professionalId.set('');
    this.reload();
  }

  setProfessional(value: string) {
    this.professionalId.set(value);
    this.reload();
  }

  setStatus(value: string) {
    this.statusFilter.set(value);
    this.reload();
  }

  setPast(value: boolean) {
    this.past.set(value);
    this.reload();
  }

  // The patient box searches as you type and filters once the text matches one of the suggestions.
  onPatient(value: string) {
    this.patientText.set(value);
    const match = this.patientMatches().find((item) => this.patientLabel(item) === value);
    if (match) {
      if (match.patientId !== this.patientId()) {
        this.patientId.set(match.patientId);
        this.reload();
      }
      return;
    }
    if (this.patientId()) {
      this.patientId.set('');
      this.reload();
    }
    const term = value.trim();
    if (term.length < 2) {
      this.patientMatches.set([]);
      return;
    }
    queueSearch(this.patientHost, () => void this.searchPatients(term));
  }

  patientLabel(item: PatientLookupDto) {
    return `${item.firstName} ${item.lastName} · ${item.documentNumber}`;
  }

  clearFilters() {
    this.query.set('');
    this.specialtyId.set('');
    this.professionalId.set('');
    this.patientId.set('');
    this.patientText.set('');
    this.patientMatches.set([]);
    this.statusFilter.set('');
    this.reload();
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  async openDetail(item: AppointmentDto) {
    this.detail.set(item);
    this.changes.set([]);
    this.changesError.set('');
    this.changesLoading.set(true);
    try {
      this.changes.set(await firstValueFrom(this.appointments.appointmentChanges(item.id)));
    } catch (error) {
      this.changesError.set(errorMessage(error));
    } finally {
      this.changesLoading.set(false);
    }
  }

  closeDetail() {
    this.detail.set(null);
  }

  // A step that only moved the slot reads as a move, not as the same status twice.
  stepLabel(change: AppointmentChangeDto, index: number) {
    const previous = index > 0 ? this.changes()[index - 1] : null;
    if (!previous) return `Turno dado como ${appointmentStatusLabels[change.status].toLowerCase()}`;
    if (previous.status === change.status) return 'Cambio de horario o profesional';
    return `Pasó a ${appointmentStatusLabels[change.status].toLowerCase()}`;
  }

  movedSlot(change: AppointmentChangeDto, index: number) {
    const previous = index > 0 ? this.changes()[index - 1] : null;
    return !previous || previous.start !== change.start || previous.professional !== change.professional;
  }

  canMove(item: AppointmentDto) {
    return item.status === AppointmentStatus.Booked || item.status === AppointmentStatus.Rescheduled || item.status === AppointmentStatus.CheckedIn;
  }

  async startMove(item: AppointmentDto) {
    this.moving.set(item);
    this.moveDate = item.start.slice(0, 10);
    this.slots.set([]);
    this.slotsError.set('');
    await this.loadSlots();
  }

  cancelMove() {
    this.moving.set(null);
    this.slots.set([]);
    this.slotsError.set('');
  }

  async loadSlots() {
    const item = this.moving();
    if (!item || !this.moveDate) return;
    this.slotsLoading.set(true);
    this.slotsError.set('');
    try {
      const slots = await firstValueFrom(this.schedule.availability({
        locationId: item.locationId,
        specialtyId: '00000000-0000-0000-0000-000000000000',
        professionalId: item.professionalId,
        date: this.moveDate,
      }));
      this.slots.set(slots.filter((slot) => slot.appointmentTypeId === item.appointmentTypeId));
    } catch (error) {
      this.slots.set([]);
      this.slotsError.set(errorMessage(error));
    } finally {
      this.slotsLoading.set(false);
    }
  }

  async moveTo(slot: SlotDto) {
    const item = this.moving();
    if (!item) return;
    this.pending.set(item.id);
    try {
      await firstValueFrom(this.appointments.rescheduleAppointment(item.id, { start: slot.start }));
      this.toasts.success('Turno reprogramado', `${item.patient} quedó para el ${formatFecha(slot.start)}.`);
      this.cancelMove();
      await this.load();
    } catch (error) {
      this.toasts.error('No se pudo reprogramar', errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  private reload() {
    this.page.set(1);
    void this.load();
  }

  private filter(): AppointmentHistoryFilter {
    return {
      specialtyId: this.specialtyId() || undefined,
      professionalId: this.professionalId() || undefined,
      patientId: this.patientId() || undefined,
      status: (this.statusFilter() as AppointmentStatus) || undefined,
      past: this.past() || undefined,
    };
  }

  private async searchPatients(term: string) {
    try {
      this.patientMatches.set(await firstValueFrom(this.patientsApi.searchPatients({ q: term })));
    } catch {
      this.patientMatches.set([]);
    }
  }

  private async loadFilters() {
    try {
      const [specialties, professionals] = await Promise.all([
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
      ]);
      this.specialties.set(specialties);
      this.professionals.set(professionals);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  private async load() {
    try {
      const result = await firstValueFrom(this.appointments.appointmentsPage({
        ...listParams(this.page(), this.sort(), this.query()),
        ...this.filter(),
      }));
      this.rows.set(result.items);
      this.total.set(result.total);
      this.page.set(result.page);
      this.error.set('');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
