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

const dayLabels: Record<string, string> = {
  Monday: 'Lunes',
  Tuesday: 'Martes',
  Wednesday: 'Miércoles',
  Thursday: 'Jueves',
  Friday: 'Viernes',
  Saturday: 'Sábado',
};

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiSelect, UiSkeleton],
  templateUrl: './turno-detail.html',
  styleUrl: './turno-detail.css',
})
export class TurnoDetailPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly schedule = inject(ScheduleService);
  private readonly params = toSignal(inject(ActivatedRoute).paramMap);
  private request = 0;
  readonly dayOptions: SelectOption[] = [
    { value: 'Monday', label: 'Lunes' },
    { value: 'Tuesday', label: 'Martes' },
    { value: 'Wednesday', label: 'Miércoles' },
    { value: 'Thursday', label: 'Jueves' },
    { value: 'Friday', label: 'Viernes' },
    { value: 'Saturday', label: 'Sábado' },
  ];
  readonly turno = signal<AppointmentTypeDto | null>(null);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly schedules = signal<Record<string, ScheduleBlockDto[]>>({});
  readonly error = signal('');
  readonly loading = signal(true);
  readonly missing = signal(false);
  readonly busy = signal('');
  readonly specialtyPick = signal('');
  readonly personId = signal('');
  private locationId = '';
  day: DayOfWeek = 'Monday';
  from = '09:00';
  to = '13:00';

  constructor() {
    effect(() => {
      const id = this.params()?.get('turnoId');
      if (id) untracked(() => void this.load(id));
    });
  }

  specialtyOptions(): SelectOption[] {
    return this.specialties().map((item) => ({ value: item.id, label: item.name }));
  }

  specialtyName(id: string | null | undefined) {
    if (!id) return '';
    return this.specialties().find((item) => item.id === id)?.name ?? '';
  }

  members() {
    const specialtyId = this.turno()?.specialtyId;
    if (!specialtyId) return [];
    return this.professionals().filter((person) => person.specialtyId === specialtyId);
  }

  memberOptions(): SelectOption[] {
    return this.members().map((person) => ({ value: person.id, label: `${person.lastName}, ${person.firstName}` }));
  }

  assigned() {
    const typeId = this.turno()?.id;
    if (!typeId) return [];
    return this.members().filter((person) => this.shiftsOf(person.id).length > 0);
  }

  shiftsOf(personId: string) {
    const typeId = this.turno()?.id;
    return (this.schedules()[personId] ?? []).filter((block) => block.appointmentTypeId === typeId);
  }

  dayLabel(day: string) {
    return dayLabels[day] ?? day;
  }

  clock(value: string) {
    return value.slice(0, 5);
  }

  async assignSpecialty() {
    const turno = this.turno();
    const specialtyId = this.specialtyPick();
    if (!turno || !specialtyId || specialtyId === turno.specialtyId) return;
    await this.run('specialty', async () => {
      await firstValueFrom(
        this.organization.updateAppointmentType(turno.id, {
          name: turno.name,
          durationMinutes: turno.durationMinutes,
          specialtyId,
        }),
      );
    });
  }

  async assignPerson() {
    const turno = this.turno();
    const personId = this.personId();
    if (!turno?.specialtyId) {
      this.error.set('Primero asigná la especialidad.');
      return;
    }
    if (!personId) return;
    if (!this.locationId) {
      this.error.set('Hace falta una sede para asignar el profesional.');
      return;
    }
    const startTime = this.from.length === 5 ? `${this.from}:00` : this.from;
    const endTime = this.to.length === 5 ? `${this.to}:00` : this.to;
    const blocks = [
      ...(this.schedules()[personId] ?? []),
      {
        id: crypto.randomUUID(),
        day: this.day,
        startTime,
        endTime,
        locationId: this.locationId,
        appointmentTypeId: turno.id,
      },
    ];
    await this.run('person', async () => {
      const saved = await firstValueFrom(this.schedule.saveSchedule({ professionalId: personId, blocks }));
      this.schedules.update((current) => ({ ...current, [personId]: saved }));
      this.personId.set('');
    });
  }

  async removeShift(personId: string, blockId: string) {
    const blocks = (this.schedules()[personId] ?? []).filter((block) => block.id !== blockId);
    await this.run(`shift:${blockId}`, async () => {
      const saved = await firstValueFrom(this.schedule.saveSchedule({ professionalId: personId, blocks }));
      this.schedules.update((current) => ({ ...current, [personId]: saved }));
    });
  }

  private async load(id: string) {
    const ticket = ++this.request;
    const switching = this.turno()?.id !== id;
    if (switching) {
      this.turno.set(null);
      this.loading.set(true);
    }
    this.error.set('');
    this.missing.set(false);
    try {
      const [locations, specialties, professionals, types] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
        firstValueFrom(this.organization.appointmentTypes()),
      ]);
      if (ticket !== this.request) return;
      const turno = types.find((item) => item.id === id) ?? null;
      this.locationId = locations.find((location) => location.isActive)?.id ?? locations[0]?.id ?? '';
      this.specialties.set(specialties);
      this.professionals.set(professionals);
      this.turno.set(turno);
      this.missing.set(!turno);
      this.specialtyPick.set(turno?.specialtyId ?? '');
      const members = professionals.filter((person) => turno?.specialtyId && person.specialtyId === turno.specialtyId);
      const blocks = await Promise.all(members.map((person) => firstValueFrom(this.schedule.getSchedule({ professionalId: person.id }))));
      if (ticket !== this.request) return;
      this.schedules.set(Object.fromEntries(members.map((person, index) => [person.id, blocks[index]])));
    } catch (error) {
      if (ticket !== this.request) return;
      this.error.set(errorMessage(error));
    } finally {
      if (ticket === this.request) this.loading.set(false);
    }
  }

  private async run(key: string, action: () => Promise<void>) {
    const id = this.turno()?.id;
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
