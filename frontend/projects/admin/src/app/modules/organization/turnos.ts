import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AppointmentTypeDto, EmployeesService, OrganizationService, ScheduleBlockDto, ScheduleService, SpecialtyDto, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './turnos.html',
})
export class TurnosPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly schedule = inject(ScheduleService);
  private readonly router = inject(Router);
  readonly types = signal<AppointmentTypeDto[]>([]);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly schedules = signal<ScheduleBlockDto[][]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly rows = computed(() =>
    this.types().map((item) => ({
      ...item,
      specialtyName: this.specialties().find((specialty) => specialty.id === item.specialtyId)?.name ?? '',
      people: this.schedules().filter((blocks) => blocks.some((block) => block.appointmentTypeId === item.id)).length,
    })),
  );
  typeName = '';
  duration = 30;

  constructor() {
    void this.load();
  }

  countLabel(count: number, one: string, many: string) {
    return count === 1 ? `1 ${one}` : `${count} ${many}`;
  }

  async createTurno() {
    const name = this.typeName.trim();
    if (!name) return;
    this.error.set('');
    this.saving.set(true);
    try {
      const created = await firstValueFrom(
        this.organization.createAppointmentType({ name, durationMinutes: Number(this.duration || 30), specialtyId: null }),
      );
      await this.router.navigate(['/organizacion/turnos', created.id]);
    } catch (error) {
      this.error.set(errorMessage(error));
      this.saving.set(false);
    }
  }

  private async load() {
    try {
      const [types, specialties, professionals] = await Promise.all([
        firstValueFrom(this.organization.appointmentTypes()),
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
      ]);
      const schedules = await Promise.all(
        professionals.map((person) => firstValueFrom(this.schedule.getSchedule({ professionalId: person.id }))),
      );
      this.types.set(types);
      this.specialties.set(specialties);
      this.schedules.set(schedules);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
