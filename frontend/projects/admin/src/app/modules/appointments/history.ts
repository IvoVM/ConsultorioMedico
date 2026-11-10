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
  SaveAppointmentChangeCommand,
  ScheduleService,
  SlotDto,
  SpecialtyDto,
  TenantRole,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { FechaPipe, MediaWatcherService, SelectOption, ToastService, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable, UiTabs, HistoryTab, historyTabLabels, TabItem, siteLabel, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { appointmentStatusLabels, appointmentStatusTone } from './models/appointment-status-labels';
import { appointmentCreationLabels, systemCreationLabel } from './models/appointment-creation-labels';

const anyOption: SelectOption = { value: '', label: 'Todos' };

// A datetime-local input speaks the browser's wall clock without an offset; the API speaks ISO instants.
function toLocalInput(iso: string) {
  const date = new Date(iso);
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}

function fromLocalInput(value: string) {
  return new Date(value).toISOString();
}

interface ChangeDraft {
  id: string | null;
  status: AppointmentStatus;
  start: string;
  end: string;
  professionalId: string;
  at: string;
}

@Component({
  imports: [FormsModule, UiTabs, RouterLink, FechaPipe, SortCol, TablePager, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable],
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
  readonly tab = signal<string>(HistoryTab.Turnos);
  // With more than one active site each row names where the visit is; otherwise it reads "Sede única".
  readonly multiSite = signal(false);
  readonly removed = computed(() => this.tab() === HistoryTab.Eliminados);
  readonly tabs: TabItem[] = [
    { key: HistoryTab.Turnos, label: historyTabLabels[HistoryTab.Turnos], icon: 'event' },
    { key: HistoryTab.Eliminados, label: historyTabLabels[HistoryTab.Eliminados], icon: 'delete' },
  ];
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
    ...(Object.keys(appointmentStatusLabels) as AppointmentStatus[])
      // Removed bookings have their own tab, so the state picker only offers the ones still in the flow.
      .filter((status) => status !== AppointmentStatus.Cancelled)
      .map((status) => ({ value: status, label: appointmentStatusLabels[status] })),
  ];
  readonly filtered = computed(() => !!(this.query() || this.specialtyId() || this.professionalId() || this.patientId() || this.statusFilter()));

  readonly detail = signal<AppointmentDto | null>(null);
  readonly changes = signal<AppointmentChangeDto[]>([]);
  readonly changesLoading = signal(false);
  readonly changesError = signal('');
  // The admin may write over the trail however they want; front desk only reads it.
  readonly canEditTrail = clinicaSession.role() === TenantRole.TenantAdmin;
  readonly draft = signal<ChangeDraft | null>(null);
  readonly savingChange = signal(false);
  readonly changeStatusOptions: SelectOption[] = (Object.keys(appointmentStatusLabels) as AppointmentStatus[]).map((status) => ({
    value: status,
    label: appointmentStatusLabels[status],
  }));
  readonly changeProfessionalOptions = computed<SelectOption[]>(() =>
    this.professionals().map((item) => ({ value: item.id, label: `${item.firstName} ${item.lastName}`.trim() })),
  );

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

  site(item: AppointmentDto) {
    return siteLabel(item.location, this.multiSite());
  }

  setTab(value: string) {
    this.tab.set(value);
    this.statusFilter.set('');
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
    this.draft.set(null);
  }

  editChange(change: AppointmentChangeDto) {
    this.draft.set({
      id: change.id,
      status: change.status,
      start: toLocalInput(change.start),
      end: toLocalInput(change.end),
      professionalId: change.professionalId,
      at: toLocalInput(change.at),
    });
  }

  newChange() {
    const item = this.detail();
    if (!item) return;
    const last = this.changes().at(0);
    this.draft.set({
      id: null,
      status: last?.status ?? item.status,
      start: toLocalInput(last?.start ?? item.start),
      end: toLocalInput(last?.end ?? item.end),
      professionalId: last?.professionalId ?? item.professionalId,
      at: toLocalInput(new Date().toISOString()),
    });
  }

  patchDraft(patch: Partial<ChangeDraft>) {
    this.draft.update((current) => (current ? { ...current, ...patch } : current));
  }

  async saveChange() {
    const item = this.detail();
    const draft = this.draft();
    if (!item || !draft) return;
    const body: SaveAppointmentChangeCommand = {
      status: draft.status,
      start: fromLocalInput(draft.start),
      end: fromLocalInput(draft.end),
      professionalId: draft.professionalId,
      at: fromLocalInput(draft.at),
    };
    this.savingChange.set(true);
    try {
      const call = draft.id
        ? this.appointments.updateAppointmentChange(item.id, draft.id, body)
        : this.appointments.addAppointmentChange(item.id, body);
      this.changes.set(await firstValueFrom(call));
      this.draft.set(null);
      this.toasts.success('Historial actualizado', draft.id ? 'El movimiento quedó modificado.' : 'El movimiento quedó agregado.');
    } catch (error) {
      this.toasts.error('No se pudo guardar el movimiento', errorMessage(error));
    } finally {
      this.savingChange.set(false);
    }
  }

  async deleteChange(change: AppointmentChangeDto) {
    const item = this.detail();
    if (!item || !confirm('¿Borrar este movimiento del historial?')) return;
    this.savingChange.set(true);
    try {
      this.changes.set(await firstValueFrom(this.appointments.deleteAppointmentChange(item.id, change.id)));
      if (this.draft()?.id === change.id) this.draft.set(null);
      this.toasts.success('Historial actualizado', 'El movimiento quedó borrado.');
    } catch (error) {
      this.toasts.error('No se pudo borrar el movimiento', errorMessage(error));
    } finally {
      this.savingChange.set(false);
    }
  }

  // Newest step first, so the one before each step is the next in the list. A step that only moved the slot reads as a move.
  stepLabel(change: AppointmentChangeDto, index: number) {
    const previous = this.changes()[index + 1] ?? null;
    if (!previous) return change.byRole ? appointmentCreationLabels[change.byRole] : systemCreationLabel;
    if (previous.status === change.status) return 'Cambio de horario o profesional';
    return `Pasó a ${appointmentStatusLabels[change.status].toLowerCase()}`;
  }

  movedSlot(change: AppointmentChangeDto, index: number) {
    const previous = this.changes()[index + 1] ?? null;
    return !previous || previous.start !== change.start || previous.professional !== change.professional;
  }

  canMove(item: AppointmentDto) {
    return item.status === AppointmentStatus.Booked || item.status === AppointmentStatus.Rescheduled;
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
      cancelled: this.removed() || undefined,
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
      const [specialties, professionals, sites] = await Promise.all([
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
        firstValueFrom(this.organization.locations()),
      ]);
      this.specialties.set(specialties);
      this.professionals.set(professionals);
      this.multiSite.set(sites.filter((item) => item.isActive).length > 1);
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
