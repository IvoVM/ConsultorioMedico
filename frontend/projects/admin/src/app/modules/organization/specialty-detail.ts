import { Component, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  AppointmentTypeDto,
  DayOfWeek,
  EmployeesService,
  OrganizationService,
  ProfessionalDto,
  ScheduleBlockDto,
  ScheduleService,
  SpecialtyDto,
  errorMessage,
} from 'sdk';
import { SelectOption, UiButton, UiEmpty, UiField, UiSelect, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';

const dayOptions: SelectOption[] = [
  { value: 'Monday', label: 'Lunes' },
  { value: 'Tuesday', label: 'Martes' },
  { value: 'Wednesday', label: 'Miércoles' },
  { value: 'Thursday', label: 'Jueves' },
  { value: 'Friday', label: 'Viernes' },
  { value: 'Saturday', label: 'Sábado' },
];

const dayLabels: Record<string, string> = Object.fromEntries(dayOptions.map((option) => [option.value, option.label]));

interface ShiftDraft {
  typeId: string;
  day: DayOfWeek;
  from: string;
  to: string;
}

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiSelect, UiSkeleton],
  templateUrl: './specialty-detail.html',
  styleUrl: './specialty-detail.css',
})
export class SpecialtyDetailPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly schedule = inject(ScheduleService);
  private readonly params = toSignal(inject(ActivatedRoute).paramMap);
  private request = 0;
  readonly dayOptions = dayOptions;
  readonly specialty = signal<SpecialtyDto | null>(null);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly appointmentTypes = signal<AppointmentTypeDto[]>([]);
  readonly schedules = signal<Record<string, ScheduleBlockDto[]>>({});
  readonly error = signal('');
  readonly loading = signal(true);
  readonly missing = signal(false);
  readonly busy = signal('');
  readonly personId = signal('');
  readonly drafts = signal<Record<string, ShiftDraft>>({});
  typeName = '';
  typeMinutes = 30;
  private locationId = '';

  constructor() {
    effect(() => {
      const id = this.params()?.get('specialtyId');
      if (id) untracked(() => void this.load(id));
    });
  }

  people() {
    const id = this.specialty()?.id;
    return this.professionals().filter((person) => person.specialtyId === id);
  }

  types() {
    const id = this.specialty()?.id;
    return this.appointmentTypes().filter((type) => type.specialtyId === id);
  }

  shiftsOf(personId: string) {
    const typeIds = new Set(this.types().map((type) => type.id));
    return (this.schedules()[personId] ?? []).filter((block) => typeIds.has(block.appointmentTypeId));
  }

  availablePeople(): SelectOption[] {
    const id = this.specialty()?.id;
    return this.professionals()
      .filter((person) => person.specialtyId !== id)
      .map((person) => ({
        value: person.id,
        label: person.specialtyId ? `${person.lastName}, ${person.firstName} · ${this.specialtyName(person.specialtyId)}` : `${person.lastName}, ${person.firstName}`,
      }));
  }

  typeOptions(): SelectOption[] {
    return this.types().map((type) => ({ value: type.id, label: `${type.name} · ${type.durationMinutes} min` }));
  }

  specialtyName(id: string) {
    return this.specialties().find((item) => item.id === id)?.name ?? '';
  }

  typeLabel(id: string) {
    return this.appointmentTypes().find((type) => type.id === id)?.name ?? 'Turno';
  }

  dayLabel(day: string) {
    return dayLabels[day] ?? day;
  }

  clock(value: string) {
    return value.slice(0, 5);
  }

  draft(personId: string): ShiftDraft {
    return this.drafts()[personId] ?? { typeId: '', day: 'Monday', from: '09:00', to: '13:00' };
  }

  patchDraft(personId: string, patch: { typeId?: string; day?: string; from?: string; to?: string }) {
    const current = this.draft(personId);
    this.drafts.update((map) => ({
      ...map,
      [personId]: {
        typeId: patch.typeId ?? current.typeId,
        day: (patch.day ?? current.day) as DayOfWeek,
        from: patch.from ?? current.from,
        to: patch.to ?? current.to,
      },
    }));
  }

  async createType() {
    const specialty = this.specialty();
    const name = this.typeName.trim();
    if (!specialty || !name) return;
    const durationMinutes = Number(this.typeMinutes || 30);
    await this.run('type', async () => {
      await firstValueFrom(this.organization.createAppointmentType({ name, durationMinutes, specialtyId: specialty.id }));
      this.typeName = '';
      this.typeMinutes = 30;
    });
  }

  async addPerson() {
    const specialty = this.specialty();
    const personId = this.personId();
    if (!specialty || !personId) return;
    await this.run('person', async () => {
      await firstValueFrom(this.employees.assignSpecialty(personId, { specialtyId: specialty.id }));
      this.personId.set('');
    });
  }

  async removePerson(person: ProfessionalDto) {
    await this.run(`remove:${person.id}`, async () => {
      await firstValueFrom(this.employees.assignSpecialty(person.id, { specialtyId: null }));
    });
  }

  async assignShift(person: ProfessionalDto) {
    const draft = this.draft(person.id);
    if (!draft.typeId) {
      this.error.set('Elegí el turno que va a atender.');
      return;
    }
    if (!this.locationId) {
      this.error.set('Hace falta una sede para asignar el turno.');
      return;
    }
    const startTime = draft.from.length === 5 ? `${draft.from}:00` : draft.from;
    const endTime = draft.to.length === 5 ? `${draft.to}:00` : draft.to;
    const blocks = [
      ...(this.schedules()[person.id] ?? []),
      {
        id: crypto.randomUUID(),
        day: draft.day,
        startTime,
        endTime,
        locationId: this.locationId,
        appointmentTypeId: draft.typeId,
      },
    ];
    await this.run(`shift:${person.id}`, async () => {
      const saved = await firstValueFrom(this.schedule.saveSchedule({ professionalId: person.id, blocks }));
      this.schedules.update((current) => ({ ...current, [person.id]: saved }));
    });
  }

  async removeShift(personId: string, blockId: string) {
    const blocks = (this.schedules()[personId] ?? []).filter((block) => block.id !== blockId);
    await this.run(`shift:${personId}`, async () => {
      const saved = await firstValueFrom(this.schedule.saveSchedule({ professionalId: personId, blocks }));
      this.schedules.update((current) => ({ ...current, [personId]: saved }));
    });
  }

  private async load(id: string) {
    const ticket = ++this.request;
    const switching = this.specialty()?.id !== id;
    if (switching) {
      this.specialty.set(null);
      this.loading.set(true);
    }
    this.error.set('');
    this.missing.set(false);
    try {
      const [locations, specialties, professionals, appointmentTypes] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
        firstValueFrom(this.organization.appointmentTypes()),
      ]);
      if (ticket !== this.request) return;
      const specialty = specialties.find((item) => item.id === id) ?? null;
      this.locationId = locations.find((location) => location.isActive)?.id ?? locations[0]?.id ?? '';
      this.specialties.set(specialties);
      this.specialty.set(specialty);
      this.missing.set(!specialty);
      this.professionals.set(professionals);
      this.appointmentTypes.set(appointmentTypes);
      const assigned = professionals.filter((person) => person.specialtyId === id);
      const blocks = await Promise.all(assigned.map((person) => firstValueFrom(this.schedule.getSchedule({ professionalId: person.id }))));
      if (ticket !== this.request) return;
      this.schedules.set(Object.fromEntries(assigned.map((person, index) => [person.id, blocks[index]])));
    } catch (error) {
      if (ticket !== this.request) return;
      this.error.set(errorMessage(error));
    } finally {
      if (ticket === this.request) this.loading.set(false);
    }
  }

  private async run(key: string, action: () => Promise<void>) {
    const id = this.specialty()?.id;
    if (!id) return;
    this.error.set('');
    this.busy.set(key);
    try {
      await action();
      await this.load(id);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.busy.set('');
    }
  }
}
