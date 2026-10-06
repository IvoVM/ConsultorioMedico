import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AppointmentTypeDto,
  BlockoutDto,
  ClinicaClient,
  DayOfWeek,
  LocationDto,
  ProfessionalDto,
  ScheduleBlockDto,
  dayLabels,
  errorMessage,
} from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './schedules.html',
})
export class SchedulesPage {
  private readonly api = inject(ClinicaClient);
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
        firstValueFrom(this.api.professionals({})),
        firstValueFrom(this.api.locations()),
        firstValueFrom(this.api.appointmentTypes()),
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
      firstValueFrom(this.api.getSchedule({ professionalId: this.professionalId })),
      firstValueFrom(this.api.blockouts({ professionalId: this.professionalId })),
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
      const blocks = await firstValueFrom(this.api.saveSchedule({ professionalId: this.professionalId, blocks: this.blocks() }));
      this.blocks.set(blocks);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async createBlockout() {
    this.error.set('');
    try {
      await firstValueFrom(
        this.api.createBlockout({
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
    await firstValueFrom(this.api.deleteBlockout(blockout.id));
    await this.loadSchedule();
  }
}
