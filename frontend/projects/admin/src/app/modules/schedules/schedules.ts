import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AppointmentTypeDto,
  BlockoutDto,
  DayOfWeek,
  EmployeesService,
  OrganizationService,
  ProfessionalDto,
  ScheduleBlockDto,
  ScheduleService,
  errorMessage,
} from 'sdk';
import { FechaPipe, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { firstValueFrom } from 'rxjs';
import { dayLabels } from './models/day-labels';

@Component({
  imports: [FormsModule, FechaPipe, ScreenNote, SortCol, TablePager, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './schedules.html',
})
export class SchedulesPage {
  private readonly employees = inject(EmployeesService);
  private readonly organization = inject(OrganizationService);
  private readonly schedule = inject(ScheduleService);
  private readonly blockSearch: { timer?: ReturnType<typeof setTimeout> } = {};
  private readonly blockoutSearch: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly dayLabels = dayLabels;
  readonly days: DayOfWeek[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly appointmentTypes = signal<AppointmentTypeDto[]>([]);
  readonly professionalOptions = computed(() =>
    this.professionals().map((pro) => ({ value: pro.id, label: `${pro.lastName}, ${pro.firstName}` })),
  );
  readonly dayOptions = this.days.map((day) => ({ value: day, label: dayLabels[day] }));
  readonly typeOptions = computed(() => this.appointmentTypes().map((type) => ({ value: type.id, label: type.name })));
  readonly blocks = signal<ScheduleBlockDto[]>([]);
  readonly blockRows = signal<ScheduleBlockDto[]>([]);
  readonly blockTotal = signal(0);
  readonly blockPage = signal(1);
  readonly blockouts = signal<BlockoutDto[]>([]);
  readonly blockoutRows = signal<BlockoutDto[]>([]);
  readonly blockoutTotal = signal(0);
  readonly blockoutPage = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly scheduleLoading = signal(false);
  readonly publishing = signal(false);
  readonly blocking = signal(false);
  readonly pending = signal('');
  readonly addingBlock = signal(false);
  readonly addingBlockout = signal(false);
  readonly blockQuery = signal('');
  readonly blockoutQuery = signal('');
  readonly blockSort = signal<SortState>({ key: 'day', dir: 'asc' });
  readonly blockoutSort = signal<SortState>({ key: 'start', dir: 'asc' });
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
      this.appointmentTypes.set(appointmentTypes);
      this.professionalId = professionals[0]?.id ?? '';
      this.locationId = locations[0]?.id ?? '';
      this.appointmentTypeId = appointmentTypes[0]?.id ?? '';
      if (this.professionalId) await this.loadSchedule();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async loadSchedule() {
    if (!this.professionalId) return;
    if (!this.loading()) this.scheduleLoading.set(true);
    try {
      const [blocks, blockPage, blockoutPage] = await Promise.all([
        firstValueFrom(this.schedule.getSchedule({ professionalId: this.professionalId })),
        firstValueFrom(this.schedule.schedulePage({ professionalId: this.professionalId, ...listParams(this.blockPage(), this.blockSort(), this.blockQuery()) })),
        firstValueFrom(this.schedule.blockoutsPage({ professionalId: this.professionalId, ...listParams(this.blockoutPage(), this.blockoutSort(), this.blockoutQuery()) })),
      ]);
      this.blocks.set(blocks);
      this.blockRows.set(blockPage.items);
      this.blockTotal.set(blockPage.total);
      this.blockPage.set(blockPage.page);
      this.blockoutRows.set(blockoutPage.items);
      this.blockoutTotal.set(blockoutPage.total);
      this.blockoutPage.set(blockoutPage.page);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.scheduleLoading.set(false);
    }
  }

  sortBlocks(key: string) {
    this.blockSort.update((current) => nextSort(current, key));
    this.blockPage.set(1);
    void this.loadSchedule();
  }

  sortBlockouts(key: string) {
    this.blockoutSort.update((current) => nextSort(current, key));
    this.blockoutPage.set(1);
    void this.loadSchedule();
  }

  onBlockSearch(value: string) {
    this.blockQuery.set(value);
    this.blockPage.set(1);
    queueSearch(this.blockSearch, () => void this.loadSchedule());
  }

  onBlockoutSearch(value: string) {
    this.blockoutQuery.set(value);
    this.blockoutPage.set(1);
    queueSearch(this.blockoutSearch, () => void this.loadSchedule());
  }

  goBlocks(page: number) {
    this.blockPage.set(page);
    void this.loadSchedule();
  }

  goBlockouts(page: number) {
    this.blockoutPage.set(page);
    void this.loadSchedule();
  }

  pickProfessional() {
    this.blockPage.set(1);
    this.blockoutPage.set(1);
    void this.loadSchedule();
  }

  async addBlock() {
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
    this.addingBlock.set(false);
    await this.save();
  }

  async removeBlock(id: string) {
    this.blocks.update((list) => list.filter((block) => block.id !== id));
    await this.save();
  }

  async save() {
    this.error.set('');
    this.publishing.set(true);
    try {
      const blocks = await firstValueFrom(this.schedule.saveSchedule({ professionalId: this.professionalId, blocks: this.blocks() }));
      this.blocks.set(blocks);
      await this.loadSchedule();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.publishing.set(false);
    }
  }

  async createBlockout() {
    this.error.set('');
    this.blocking.set(true);
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
      this.addingBlockout.set(false);
      await this.loadSchedule();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.blocking.set(false);
    }
  }

  async deleteBlockout(blockout: BlockoutDto) {
    this.pending.set(blockout.id);
    try {
      await firstValueFrom(this.schedule.deleteBlockout(blockout.id));
      await this.loadSchedule();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }
}
