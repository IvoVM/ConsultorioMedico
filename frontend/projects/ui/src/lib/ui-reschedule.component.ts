import { Component, input, output, signal } from '@angular/core';
import { UiButton } from './ui-button.component';
import { UiIcon } from './ui-icon.component';
import { UiModal } from './ui-modal.component';
import { UiMonth } from './ui-month.component';
import { formatFecha } from './fecha';

export type RescheduleSlot = { start: string };

export enum RescheduleOrigin {
  Nearest = 'nearest',
  Manual = 'manual',
}

export type RescheduleSource = {
  days(year: number, month: number): Promise<string[]>;
  slots(date: string): Promise<RescheduleSlot[]>;
  nearest(): Promise<RescheduleSlot>;
  confirm(start: string): Promise<void>;
};

function failure(error: unknown, fallback: string) {
  return error instanceof Error && error.message ? error.message : fallback;
}

@Component({
  selector: 'ui-reschedule',
  imports: [UiButton, UiIcon, UiModal, UiMonth],
  templateUrl: './ui-reschedule.component.html',
  styleUrl: './ui-reschedule.component.css',
})
export class UiReschedule {
  readonly title = input('Reprogramar');
  readonly detail = input('');
  readonly source = input.required<RescheduleSource>();
  readonly closed = output<void>();

  readonly origins = RescheduleOrigin;
  readonly year = signal(new Date().getFullYear());
  readonly month = signal(new Date().getMonth() + 1);
  readonly days = signal<string[]>([]);
  readonly day = signal('');
  readonly slots = signal<RescheduleSlot[]>([]);
  readonly chosen = signal<RescheduleSlot | null>(null);
  readonly origin = signal<RescheduleOrigin>(RescheduleOrigin.Manual);
  readonly loadingDays = signal(false);
  readonly loadingSlots = signal(false);
  readonly searching = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');

  constructor() {
    queueMicrotask(() => void this.loadDays());
  }

  dayLabel(iso: string) {
    return formatFecha(iso, 'dia');
  }

  timeLabel(start: string) {
    return formatFecha(start, 'hora');
  }

  async shift(delta: number) {
    const next = new Date(this.year(), this.month() - 1 + delta, 1);
    this.year.set(next.getFullYear());
    this.month.set(next.getMonth() + 1);
    this.day.set('');
    this.slots.set([]);
    await this.loadDays();
  }

  async pickDay(iso: string) {
    this.day.set(iso);
    this.slots.set([]);
    this.loadingSlots.set(true);
    this.error.set('');
    try {
      const slots = await this.source().slots(iso);
      this.slots.set([...slots].sort((a, b) => a.start.localeCompare(b.start)));
    } catch (error) {
      this.error.set(failure(error, 'No se pudieron traer los horarios.'));
    } finally {
      this.loadingSlots.set(false);
    }
  }

  choose(slot: RescheduleSlot) {
    this.origin.set(RescheduleOrigin.Manual);
    this.chosen.set(slot);
  }

  async findNearest() {
    this.searching.set(true);
    this.error.set('');
    try {
      const slot = await this.source().nearest();
      this.origin.set(RescheduleOrigin.Nearest);
      this.chosen.set(slot);
    } catch (error) {
      this.error.set(failure(error, 'No se encontró un horario libre.'));
    } finally {
      this.searching.set(false);
    }
  }

  async back() {
    const slot = this.chosen();
    this.chosen.set(null);
    if (!slot || this.origin() !== RescheduleOrigin.Nearest) return;
    const [year, month] = slot.start.slice(0, 10).split('-').map(Number);
    if (year !== this.year() || month !== this.month()) {
      this.year.set(year);
      this.month.set(month);
      await this.loadDays();
    }
    await this.pickDay(slot.start.slice(0, 10));
  }

  async confirm() {
    const slot = this.chosen();
    if (!slot) return;
    this.saving.set(true);
    this.error.set('');
    try {
      await this.source().confirm(slot.start);
    } catch (error) {
      this.error.set(failure(error, 'No se pudo reprogramar.'));
    } finally {
      this.saving.set(false);
    }
  }

  private async loadDays() {
    this.loadingDays.set(true);
    this.error.set('');
    try {
      this.days.set(await this.source().days(this.year(), this.month()));
    } catch (error) {
      this.days.set([]);
      this.error.set(failure(error, 'No se pudieron traer los días.'));
    } finally {
      this.loadingDays.set(false);
    }
  }
}
