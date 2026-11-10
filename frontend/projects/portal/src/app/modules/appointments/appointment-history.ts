import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  AppointmentDto,
  AppointmentStatus,
  AppointmentsService,
  EmployeesService,
  OrganizationService,
  PatientLookupDto,
  PatientsService,
  ProfessionalDto,
  SpecialtyDto,
  TenantRole,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { FechaPipe, SelectOption, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable, UiTabs, HistoryTab, historyTabLabels, TabItem } from 'ui';
import { firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from './models/appointment-status-labels';
import { AppointmentChanges } from './appointment-changes';
import { RouteParam } from '../../app.routes.enum';

const pageSize = 10;
const searchDelay = 300;

@Component({
  imports: [FormsModule, UiTabs, AppointmentChanges, FechaPipe, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './appointment-history.html',
})
export class AppointmentHistoryPage {
  private readonly appointments = inject(AppointmentsService);
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly patientsApi = inject(PatientsService);
  private searchTimer?: ReturnType<typeof setTimeout>;
  private patientTimer?: ReturnType<typeof setTimeout>;
  readonly statusLabels = appointmentStatusLabels;
  // The professional only gets the appointments they attend, so the specialty, professional and patient pickers are front desk's.
  readonly doctor = clinicaSession.role() === TenantRole.Doctor;
  readonly rows = signal<AppointmentDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / pageSize)));
  readonly from = computed(() => (this.total() ? (this.page() - 1) * pageSize + 1 : 0));
  readonly to = computed(() => Math.min(this.total(), this.page() * pageSize));

  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly patientMatches = signal<PatientLookupDto[]>([]);
  readonly query = signal('');
  readonly specialtyId = signal('');
  readonly professionalId = signal('');
  readonly patientId = signal('');
  readonly patientText = signal('');
  readonly statusFilter = signal('');
  readonly tab = signal<string>(HistoryTab.Turnos);
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
    { value: '', label: 'Todos' },
    ...this.professionals()
      .filter((item) => !this.specialtyId() || item.specialtyId === this.specialtyId())
      .map((item) => ({ value: item.id, label: `${item.firstName} ${item.lastName}`.trim() })),
  ]);
  readonly statusOptions: SelectOption[] = [
    { value: '', label: 'Todos' },
    ...(Object.keys(appointmentStatusLabels) as AppointmentStatus[])
      // Removed bookings have their own tab, so the state picker only offers the ones still in the flow.
      .filter((status) => status !== AppointmentStatus.Cancelled)
      .map((status) => ({ value: status, label: appointmentStatusLabels[status] })),
  ];
  readonly filtered = computed(() => !!(this.query() || this.specialtyId() || this.professionalId() || this.patientId() || this.statusFilter()));

  readonly detail = signal<AppointmentDto | null>(null);

  constructor() {
    // Front desk lands here from a patient search with the patient already picked, and sees every appointment of theirs.
    const requested = inject(ActivatedRoute).snapshot.queryParamMap.get(RouteParam.Paciente);
    if (requested && !this.doctor) {
      this.patientId.set(requested);
      this.past.set(false);
      void this.loadPatient(requested);
    }
    void this.load();
    if (!this.doctor) void this.loadFilters();
  }

  tone(status: AppointmentStatus): 'ok' | 'warn' | 'muted' {
    if (status === AppointmentStatus.Cancelled || status === AppointmentStatus.NoShow) return 'warn';
    return status === AppointmentStatus.Completed ? 'ok' : 'muted';
  }

  onSearch(value: string) {
    this.query.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.reload(), searchDelay);
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
    clearTimeout(this.patientTimer);
    if (term.length < 2) {
      this.patientMatches.set([]);
      return;
    }
    this.patientTimer = setTimeout(() => void this.searchPatients(term), searchDelay);
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

  openDetail(item: AppointmentDto) {
    this.detail.set(item);
  }

  closeDetail() {
    this.detail.set(null);
  }

  private reload() {
    this.page.set(1);
    void this.load();
  }

  private async loadPatient(id: string) {
    try {
      const patient = await firstValueFrom(this.patientsApi.patient(id));
      this.patientMatches.set([patient]);
      this.patientText.set(this.patientLabel(patient));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
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
        page: this.page(),
        sort: 'start',
        dir: 'desc',
        q: this.query().trim() || undefined,
        specialtyId: this.specialtyId() || undefined,
        professionalId: this.professionalId() || undefined,
        patientId: this.patientId() || undefined,
        status: (this.statusFilter() as AppointmentStatus) || undefined,
        past: this.past() || undefined,
      cancelled: this.removed() || undefined,
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
