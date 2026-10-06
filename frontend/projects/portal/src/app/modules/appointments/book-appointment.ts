import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  AppointmentsService,
  EmployeesService,
  OrganizationService,
  ProfessionalDto,
  ScheduleService,
  SlotDto,
  SpecialtyDto,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { UiButton, UiEmpty, UiField, UiSelect, UiSpinner, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';
import { clearPending, readPending, savePending } from './pending-booking';

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
  private readonly router = inject(Router);
  private readonly cache = new Map<string, DayOption[]>();
  readonly session = clinicaSession;
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly days = signal<DayOption[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly held = signal<SlotDto | null>(null);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly joining = signal(false);
  readonly booking = signal('');
  readonly locationId = signal('');
  readonly specialtyId = signal('');
  readonly professionalId = signal('');
  readonly date = signal('');
  notes = '';

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
    this.forgetHeld();
    this.specialtyId.set(id);
    const selected = this.professionals().find((pro) => pro.id === this.professionalId());
    if (selected && id && selected.specialtyId !== id) this.professionalId.set('');
    void this.loadDays();
  }

  pickProfessional(id: string) {
    this.forgetHeld();
    this.professionalId.set(id);
    const specialtyId = this.professionals().find((pro) => pro.id === id)?.specialtyId;
    if (specialtyId) this.specialtyId.set(specialtyId);
    void this.loadDays();
  }

  pickDay(iso: string) {
    this.forgetHeld();
    const day = this.days().find((item) => item.iso === iso);
    if (!day) return;
    this.date.set(day.iso);
    this.slots.set(day.slots);
  }

  chooseForMe() {
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
    if (!clinicaSession.token()) {
      savePending(slot);
      this.held.set(slot);
      return;
    }
    if (clinicaSession.role() !== 'Patient') {
      this.error.set('Los turnos se reservan con una cuenta de paciente.');
      return;
    }
    await this.confirm(slot);
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
      this.error.set('Quedaste en la lista de espera.');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.joining.set(false);
    }
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
    if (!pending || clinicaSession.role() !== 'Patient') return false;
    try {
      await firstValueFrom(
        this.appointmentsApi.bookAppointment({
          professionalId: pending.professionalId,
          locationId: pending.locationId,
          appointmentTypeId: pending.appointmentTypeId,
          start: pending.start,
          patientId: null,
          visitReason: null,
        }),
      );
      clearPending();
      await this.router.navigateByUrl('/mis-turnos');
      return true;
    } catch (error) {
      this.error.set(errorMessage(error));
      clearPending();
      return false;
    }
  }

  private forgetHeld() {
    this.held.set(null);
    if (this.error() === 'Quedaste en la lista de espera.') this.error.set('');
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

  private async confirm(slot: SlotDto) {
    this.booking.set(slot.start + slot.professionalId);
    this.error.set('');
    try {
      await firstValueFrom(
        this.appointmentsApi.bookAppointment({
          professionalId: slot.professionalId,
          locationId: slot.locationId,
          appointmentTypeId: slot.appointmentTypeId,
          start: slot.start,
          patientId: null,
          visitReason: null,
        }),
      );
      clearPending();
      await this.router.navigateByUrl('/mis-turnos');
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
