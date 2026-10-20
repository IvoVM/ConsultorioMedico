import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AppointmentsService,
  ClinicalService,
  EmployeesService,
  InsurerDto,
  OrganizationService,
  ProfessionalDto,
  ScheduleService,
  SlotDto,
  SpecialtyDto,
  TenantRole,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { UiButton, UiEmpty, UiField, UiSelect, UiSpinner, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';
import { DeskPatient } from './desk-booking';
import { clearPending, readPending, savePending } from './pending-booking';
import { AppRoute } from '../../app.routes.enum';

interface DayOption {
  iso: string;
  slots: SlotDto[];
}

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiField, UiEmpty, UiSelect, UiSpinner],
  templateUrl: './book-appointment.html',
  styleUrl: './book-appointment.css',
})
export class BookAppointmentPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly schedule = inject(ScheduleService);
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly clinical = inject(ClinicalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private chart: { insurance: string; member: string } | null = null;
  private readonly cache = new Map<string, DayOption[]>();
  readonly session = clinicaSession;
  readonly paths = AppRoute;
  readonly roles = TenantRole;
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly days = signal<DayOption[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly held = signal<SlotDto | null>(null);
  readonly draft = signal<SlotDto | null>(null);
  readonly insurers = signal<InsurerDto[]>([]);
  readonly error = signal('');
  readonly notice = signal('');
  readonly loading = signal(true);
  readonly joining = signal(false);
  readonly booking = signal('');
  readonly locationId = signal('');
  readonly specialtyId = signal('');
  readonly professionalId = signal('');
  readonly date = signal('');
  notes = '';
  coverage: 'particular' | 'obra' = 'particular';
  insurerId = '';
  memberNumber = '';
  readonly bookingFor = signal('');
  private bookingPatientId: string | null = null;
  private deskDraft: DeskPatient | null = null;

  readonly nearestIso = computed(() => this.days()[0]?.iso ?? '');
  readonly specialtyOptions = computed(() => [
    { value: '', label: 'Todas' },
    ...this.specialties().map((item) => ({ value: item.id, label: item.name })),
  ]);

  readonly visibleProfessionals = computed(() => {
    const specialtyId = this.specialtyId();
    const list = this.professionals();
    if (!specialtyId) return list;
    return list.filter((pro) => pro.specialtyId === specialtyId);
  });

  readonly groups = computed(() => {
    const map = new Map<string, { id: string; name: string; type: string; slots: SlotDto[] }>();
    for (const slot of this.slots()) {
      const group = map.get(slot.professionalId) ?? { id: slot.professionalId, name: slot.professional, type: slot.appointmentType, slots: [] };
      group.slots.push(slot);
      map.set(slot.professionalId, group);
    }
    return [...map.values()];
  });

  constructor() {
    void this.prepare();
  }

  insurerOptions() {
    return this.insurers().map((item) => ({ value: item.id, label: item.name }));
  }

  specialtyName(id: string | null | undefined) {
    return this.specialties().find((item) => item.id === id)?.name ?? '';
  }

  timeLabel(value: string) {
    return formatFecha(value, 'hora');
  }

  dayTitle(iso: string) {
    return formatDay(iso, { weekday: 'long', day: 'numeric', month: 'long' });
  }

  weekday(iso: string) {
    return formatDay(iso, { weekday: 'long' });
  }

  dayNumber(iso: string) {
    return formatDay(iso, { day: 'numeric' });
  }

  async prepare() {
    if (await this.finishPending()) return;
    try {
      const [locations, specialties, professionals] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
      ]);
      this.specialties.set(specialties);
      this.professionals.set(professionals);
      this.locationId.set(locations.find((location) => location.isActive)?.id ?? locations[0]?.id ?? '');
      await this.loadChart();
      const pending = readPending();
      if (pending && !clinicaSession.token()) this.applyPending(pending, professionals);
      await this.loadDays(this.date() || undefined);
      this.restoreHeld();
    } catch (error) {
      this.error.set(errorMessage(error));
      this.loading.set(false);
    }
  }

  pickSpecialty(id: string) {
    this.cancelDraft();
    this.forgetHeld();
    this.specialtyId.set(id);
    const selected = this.professionals().find((pro) => pro.id === this.professionalId());
    if (selected && id && selected.specialtyId !== id) this.professionalId.set('');
    void this.loadDays();
  }

  pickProfessional(id: string) {
    this.cancelDraft();
    this.forgetHeld();
    this.professionalId.set(id);
    const specialtyId = this.professionals().find((pro) => pro.id === id)?.specialtyId;
    if (specialtyId) this.specialtyId.set(specialtyId);
    void this.loadDays();
  }

  pickDay(iso: string) {
    this.cancelDraft();
    this.forgetHeld();
    const day = this.days().find((item) => item.iso === iso);
    if (!day) return;
    this.date.set(day.iso);
    this.slots.set(day.slots);
  }

  chooseForMe() {
    this.cancelDraft();
    this.forgetHeld();
    this.professionalId.set('');
    this.specialtyId.set('');
    void this.loadDays();
  }

  async loadDays(prefer?: string) {
    const locationId = this.locationId();
    if (!locationId) {
      this.loading.set(false);
      return;
    }
    const key = `${this.specialtyId()}|${this.professionalId()}`;
    const cached = this.cache.get(key);
    if (cached) {
      this.applyDays(cached, prefer);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.error.set('');
    try {
      const specialtyIds = this.specialtyIds();
      let days = await this.collectDays(specialtyIds);
      if (prefer && !days.some((day) => day.iso === prefer)) {
        const extra = await this.slotsForDate(prefer, specialtyIds);
        if (extra.slots.length) days = [...days, { iso: prefer, slots: extra.slots }].sort((left, right) => left.iso.localeCompare(right.iso));
      }
      this.cache.set(key, days);
      this.applyDays(days, prefer);
      if (!days.length && !this.error()) this.error.set('');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async book(slot: SlotDto) {
    this.error.set('');
    this.coverage = 'particular';
    this.insurerId = '';
    this.memberNumber = '';
    this.draft.set(slot);
    try {
      this.insurers.set(await firstValueFrom(this.organization.professionalInsurers(slot.professionalId)));
      this.applyChart();
    } catch (error) {
      this.insurers.set([]);
      this.error.set(errorMessage(error));
    }
  }

  cancelDraft() {
    this.draft.set(null);
    this.insurers.set([]);
  }

  release() {
    this.held.set(null);
    clearPending();
  }

  async joinWaitlist() {
    this.joining.set(true);
    try {
      await firstValueFrom(
        this.appointmentsApi.joinWaitlist({
          patientId: null,
          professionalId: this.professionalId() || null,
          locationId: this.locationId(),
          specialtyId: this.specialtyId() || this.specialties()[0]?.id || '',
          notes: this.notes,
        }),
      );
      this.error.set('');
      this.notice.set('Quedaste en la lista de espera.');
    } catch (error) {
      this.notice.set('');
      this.error.set(errorMessage(error));
    } finally {
      this.joining.set(false);
    }
  }

  private async loadChart() {
    const requested = this.route.snapshot.queryParamMap.get('paciente');
    const staff = clinicaSession.role() === TenantRole.Secretary || clinicaSession.role() === TenantRole.TenantAdmin;
    const draft = history.state as Partial<DeskPatient> | null;
    if (staff && !requested && draft?.documentNumber && draft.firstName && draft.lastName && draft.phone) {
      this.deskDraft = {
        documentNumber: draft.documentNumber,
        firstName: draft.firstName,
        lastName: draft.lastName,
        phone: draft.phone,
      };
      this.bookingFor.set(`${draft.lastName}, ${draft.firstName}`);
      return;
    }
    const patientId = staff ? requested : null;
    if (staff && !patientId) {
      await this.router.navigateByUrl(AppRoute.DarTurno);
      return;
    }
    if (!staff && clinicaSession.role() !== TenantRole.Patient) return;
    this.bookingPatientId = patientId;
    try {
      const history = await firstValueFrom(this.clinical.clinicalHistory(patientId ? { patientId } : {}));
      this.bookingFor.set(patientId ? history.patient.name : '');
      this.chart = {
        insurance: history.patient.healthInsurance?.trim() ?? '',
        member: history.patient.memberNumber?.trim() ?? '',
      };
    } catch {
      this.chart = null;
    }
  }

  private applyChart() {
    const insurance = this.chart?.insurance ?? '';
    if (!insurance) return;
    const match = this.insurers().find((item) => item.name.localeCompare(insurance, 'es', { sensitivity: 'accent' }) === 0);
    if (!match || !this.chart?.member) return;
    this.coverage = 'obra';
    this.insurerId = match.id;
    this.memberNumber = this.chart.member;
  }

  private specialtyIds() {
    const selected = this.professionals().find((pro) => pro.id === this.professionalId());
    if (selected?.specialtyId) return [selected.specialtyId];
    if (this.specialtyId()) return [this.specialtyId()];
    return this.specialties().map((item) => item.id);
  }

  private async collectDays(specialtyIds: string[]) {
    const found: DayOption[] = [];
    const wanted = 6;
    const horizon = 21;
    let failures = 0;
    let attempts = 0;
    for (let start = 0; start < horizon && found.length < wanted; start += 7) {
      const dates = Array.from({ length: Math.min(7, horizon - start) }, (_, index) => isoDate(addDays(new Date(), start + index)));
      const batches = await Promise.all(dates.map((iso) => this.slotsForDate(iso, specialtyIds)));
      dates.forEach((iso, index) => {
        const batch = batches[index];
        attempts += batch.attempts;
        failures += batch.failures;
        if (batch.slots.length && found.length < wanted) found.push({ iso, slots: batch.slots });
      });
    }
    if (!found.length && attempts > 0 && failures === attempts) {
      this.error.set('No se pudo leer la agenda.');
    }
    return found;
  }

  private async slotsForDate(iso: string, specialtyIds: string[]) {
    const locationId = this.locationId();
    const professionalId = this.professionalId() || undefined;
    let failures = 0;
    const lists = await Promise.all(
      specialtyIds.map(async (specialtyId) => {
        try {
          return await firstValueFrom(
            this.schedule.availability({
              locationId,
              specialtyId,
              professionalId,
              date: iso,
            }),
          );
        } catch {
          failures += 1;
          return [] as SlotDto[];
        }
      }),
    );
    const slots = lists.flat().sort((left, right) => left.start.localeCompare(right.start));
    return { slots, attempts: specialtyIds.length, failures };
  }

  private applyDays(days: DayOption[], prefer?: string) {
    this.days.set(days);
    const picked = days.find((day) => day.iso === prefer) ?? days[0];
    this.date.set(picked?.iso ?? '');
    this.slots.set(picked?.slots ?? []);
  }

  private async finishPending() {
    const pending = readPending();
    if (!pending || clinicaSession.role() !== TenantRole.Patient) return false;
    try {
      await firstValueFrom(
        this.appointmentsApi.bookAppointment({
          professionalId: pending.professionalId,
          locationId: pending.locationId,
          appointmentTypeId: pending.appointmentTypeId,
          start: pending.start,
          patientId: null,
          visitReason: null,
          insurerId: pending.insurerId || null,
          memberNumber: pending.memberNumber || null,
        }),
      );
      clearPending();
      await this.router.navigateByUrl(AppRoute.MisTurnos);
      return true;
    } catch (error) {
      this.error.set(errorMessage(error));
      clearPending();
      return false;
    }
  }

  private forgetHeld() {
    this.held.set(null);
    this.notice.set('');
    clearPending();
  }

  private restoreHeld() {
    const pending = readPending();
    if (!pending || clinicaSession.token()) return;
    const match = this.slots().find((slot) => slot.start === pending.start && slot.professionalId === pending.professionalId);
    if (match) this.held.set(match);
  }

  private applyPending(pending: NonNullable<ReturnType<typeof readPending>>, professionals: ProfessionalDto[]) {
    this.locationId.set(pending.locationId);
    this.professionalId.set(pending.professionalId);
    this.date.set(pending.start.slice(0, 10));
    const specialtyId = professionals.find((pro) => pro.id === pending.professionalId)?.specialtyId;
    if (specialtyId) this.specialtyId.set(specialtyId);
  }

  async confirmDraft() {
    const slot = this.draft();
    if (!slot) return;
    const coverage = this.coverageChoice();
    if (coverage === false) return;
    if (!clinicaSession.token()) {
      savePending(slot, coverage);
      this.held.set(slot);
      this.draft.set(null);
      return;
    }
    if (clinicaSession.role() !== TenantRole.Patient && !this.bookingPatientId && !this.deskDraft) {
      this.error.set('Para reservar este turno, elegí al paciente desde secretaría.');
      return;
    }
    await this.confirm(slot, coverage);
  }

  private coverageChoice(): { insurerId: string | null; memberNumber: string | null } | false {
    if (this.coverage === 'particular') return { insurerId: null, memberNumber: null };
    if (!this.insurerId) {
      this.error.set('Elegí la obra social.');
      return false;
    }
    const memberNumber = this.memberNumber.trim();
    if (!memberNumber) {
      this.error.set('El número de afiliado es obligatorio.');
      return false;
    }
    return { insurerId: this.insurerId, memberNumber };
  }

  private async confirm(slot: SlotDto, coverage: { insurerId: string | null; memberNumber: string | null }) {
    this.booking.set(slot.start + slot.professionalId);
    this.error.set('');
    try {
      const draft = this.deskDraft;
      if (draft) {
        await firstValueFrom(
          this.appointmentsApi.bookAtDesk({
            documentNumber: draft.documentNumber,
            firstName: draft.firstName,
            lastName: draft.lastName,
            phone: draft.phone,
            professionalId: slot.professionalId,
            locationId: slot.locationId,
            appointmentTypeId: slot.appointmentTypeId,
            start: slot.start,
            insurerId: coverage.insurerId,
            memberNumber: coverage.memberNumber,
          }),
        );
      } else {
        await firstValueFrom(
          this.appointmentsApi.bookAppointment({
            professionalId: slot.professionalId,
            locationId: slot.locationId,
            appointmentTypeId: slot.appointmentTypeId,
            start: slot.start,
            patientId: this.bookingPatientId,
            visitReason: null,
            insurerId: coverage.insurerId,
            memberNumber: coverage.memberNumber,
          }),
        );
      }
      clearPending();
      this.draft.set(null);
      await this.router.navigateByUrl(this.bookingPatientId || draft ? AppRoute.Secretaria : AppRoute.MisTurnos);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.booking.set('');
    }
  }
}

function isoDate(date: Date) {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function addDays(date: Date, amount: number) {
  const next = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  next.setDate(next.getDate() + amount);
  return next;
}

function parseIso(iso: string) {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, (month ?? 1) - 1, day ?? 1);
}

function formatDay(iso: string, options: Intl.DateTimeFormatOptions) {
  const label = new Intl.DateTimeFormat('es-AR', options).format(parseIso(iso));
  return label.charAt(0).toUpperCase() + label.slice(1);
}
