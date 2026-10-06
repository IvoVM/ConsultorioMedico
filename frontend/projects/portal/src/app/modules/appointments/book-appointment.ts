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
import { UiButton, UiEmpty, UiField, UiIcon, UiSkeleton, UiSpinner } from 'ui';
import { firstValueFrom } from 'rxjs';
import { clearPending, readPending, savePending } from './pending-booking';

interface DayCell {
  iso: string;
  day: number;
  inMonth: boolean;
  past: boolean;
  today: boolean;
  label: string;
}

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiField, UiEmpty, UiSkeleton, UiIcon, UiSpinner],
  templateUrl: './book-appointment.html',
  styleUrl: './book-appointment.css',
})
export class BookAppointmentPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly schedule = inject(ScheduleService);
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly router = inject(Router);
  readonly session = clinicaSession;
  readonly weekdays = ['lun', 'mar', 'mié', 'jue', 'vie', 'sáb', 'dom'];
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly held = signal<SlotDto | null>(null);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly searching = signal(false);
  readonly joining = signal(false);
  readonly booking = signal('');
  readonly searched = signal(false);
  readonly locationId = signal('');
  readonly specialtyId = signal('');
  readonly professionalId = signal('');
  readonly date = signal(isoDate(new Date()));
  readonly month = signal(startOfMonth(new Date()));
  notes = '';

  readonly visibleProfessionals = computed(() => {
    const specialtyId = this.specialtyId();
    const list = this.professionals();
    if (!specialtyId) return list;
    return list.filter((pro) => pro.specialtyId === specialtyId);
  });

  readonly monthLabel = computed(() => {
    const label = new Intl.DateTimeFormat('es-AR', { month: 'long', year: 'numeric' }).format(this.month());
    return label.charAt(0).toUpperCase() + label.slice(1);
  });

  readonly canGoPrev = computed(() => {
    const month = this.month();
    const today = new Date();
    return month.getFullYear() > today.getFullYear() || (month.getFullYear() === today.getFullYear() && month.getMonth() > today.getMonth());
  });

  readonly cells = computed(() => buildCells(this.month()));

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
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return new Intl.DateTimeFormat('es-AR', { hour: '2-digit', minute: '2-digit' }).format(date);
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
      this.specialtyId.set(specialties[0]?.id ?? '');
      const pending = readPending();
      if (pending && !clinicaSession.token()) this.applyPending(pending, professionals);
      await this.search();
      this.restoreHeld();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  pickSpecialty(id: string) {
    this.forgetHeld();
    this.specialtyId.set(id);
    const selected = this.professionals().find((pro) => pro.id === this.professionalId());
    if (selected && selected.specialtyId !== id) this.professionalId.set('');
    void this.search();
  }

  pickProfessional(id: string) {
    this.forgetHeld();
    this.professionalId.set(id);
    void this.search();
  }

  pickDay(iso: string) {
    this.forgetHeld();
    this.date.set(iso);
    const [year, month] = iso.split('-').map(Number);
    this.month.set(new Date(year, month - 1, 1));
    void this.search();
  }

  shiftMonth(delta: number) {
    const next = new Date(this.month());
    next.setMonth(next.getMonth() + delta);
    if (delta < 0 && !this.canGoPrev()) return;
    this.month.set(startOfMonth(next));
  }

  async search() {
    const locationId = this.locationId();
    const specialtyId = this.specialtyId();
    if (!locationId || !specialtyId) return;
    this.searching.set(true);
    try {
      this.slots.set(
        await firstValueFrom(
          this.schedule.availability({
            locationId,
            specialtyId,
            professionalId: this.professionalId() || undefined,
            date: this.date(),
          }),
        ),
      );
      this.searched.set(true);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.searching.set(false);
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
          specialtyId: this.specialtyId(),
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
    this.error.set('');
    clearPending();
  }

  private restoreHeld() {
    const pending = readPending();
    if (!pending || clinicaSession.token()) return;
    const match = this.slots().find((slot) => slot.start === pending.start && slot.professionalId === pending.professionalId);
    if (match) this.held.set(match);
  }

  private applyPending(pending: ReturnType<typeof readPending>, professionals: ProfessionalDto[]) {
    if (!pending) return;
    this.locationId.set(pending.locationId);
    this.professionalId.set(pending.professionalId);
    const day = pending.start.slice(0, 10);
    this.date.set(day);
    const [year, month] = day.split('-').map(Number);
    this.month.set(new Date(year, (month ?? 1) - 1, 1));
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

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function buildCells(month: Date): DayCell[] {
  const first = new Date(month.getFullYear(), month.getMonth(), 1);
  const startPad = (first.getDay() + 6) % 7;
  const start = new Date(first);
  start.setDate(1 - startPad);
  const today = isoDate(new Date());
  const formatter = new Intl.DateTimeFormat('es-AR', { day: 'numeric', month: 'long' });
  const cells: DayCell[] = [];
  for (let index = 0; index < 42; index += 1) {
    const date = new Date(start);
    date.setDate(start.getDate() + index);
    const iso = isoDate(date);
    cells.push({
      iso,
      day: date.getDate(),
      inMonth: date.getMonth() === month.getMonth(),
      past: iso < today,
      today: iso === today,
      label: formatter.format(date),
    });
  }
  return cells;
}
