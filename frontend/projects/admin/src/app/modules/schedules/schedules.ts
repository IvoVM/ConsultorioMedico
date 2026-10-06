import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AppointmentTypeDto,
  BlockoutDto,
  DayOfWeek,
  EmployeesService,
  LocationDto,
  OrganizationService,
  ProfessionalDto,
  ScheduleBlockDto,
  ScheduleService,
  errorMessage,
} from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { dayLabels } from './models/day-labels';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './schedules.html',
})
export class SchedulesPage {
  private readonly employees = inject(EmployeesService);
  private readonly organization = inject(OrganizationService);
  private readonly schedule = inject(ScheduleService);
  readonly dayLabels = dayLabels;
  readonly days: DayOfWeek[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly locations = signal<LocationDto[]>([]);
  readonly appointmentTypes = signal<AppointmentTypeDto[]>([]);
  readonly blocks = signal<ScheduleBlockDto[]>([]);
  readonly blockouts = signal<BlockoutDto[]>([]);
  readonly error = signal('');
  professionalId = '';
  day: DayOfWeek = 'Monday';
  from = '09:00';
  to = '13:00';
  locationId = '';
  appointmentTypeId = '';
  blockoutStart = '';
  blockoutEnd = '';
  reason = '';

  constructor() {
    void this.loadBase();
  }

  async loadBase() {
    try {
      const [professionals, locations, appointmentTypes] = await Promise.all([
        firstValueFrom(this.employees.professionals({})),
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.appointmentTypes()),
      ]);
      this.professionals.set(professionals);
      this.locations.set(locations);
      this.appointmentTypes.set(appointmentTypes);
      this.professionalId = professionals[0]?.id ?? '';
      this.locationId = locations[0]?.id ?? '';
      this.appointmentTypeId = appointmentTypes[0]?.id ?? '';
      if (this.professionalId) await this.loadSchedule();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async loadSchedule() {
    if (!this.professionalId) return;
    const [blocks, blockouts] = await Promise.all([
      firstValueFrom(this.schedule.getSchedule({ professionalId: this.professionalId })),
      firstValueFrom(this.schedule.blockouts({ professionalId: this.professionalId })),
    ]);
    this.blocks.set(blocks);
    this.blockouts.set(blockouts);
  }

  locationName(id: string) {
    return this.locations().find((location) => location.id === id)?.name ?? '';
  }

  addBlock() {
    this.blocks.update((list) => [
      ...list,
      {
        id: crypto.randomUUID(),
        day: this.day,
        startTime: this.from.length === 5 ? `${this.from}:00` : this.from,
        endTime: this.to.length === 5 ? `${this.to}:00` : this.to,
        locationId: this.locationId,
        appointmentTypeId: this.appointmentTypeId,
      },
    ]);
  }

  removeBlock(index: number) {
    this.blocks.update((list) => list.filter((_, i) => i !== index));
  }

  async save() {
    this.error.set('');
    try {
      const blocks = await firstValueFrom(this.schedule.saveSchedule({ professionalId: this.professionalId, blocks: this.blocks() }));
      this.blocks.set(blocks);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async createBlockout() {
    this.error.set('');
    try {
      await firstValueFrom(
        this.schedule.createBlockout({
          professionalId: this.professionalId,
          start: new Date(this.blockoutStart).toISOString(),
          end: new Date(this.blockoutEnd).toISOString(),
          reason: this.reason,
        }),
      );
      this.reason = '';
      await this.loadSchedule();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async deleteBlockout(blockout: BlockoutDto) {
    await firstValueFrom(this.schedule.deleteBlockout(blockout.id));
    await this.loadSchedule();
  }
}
