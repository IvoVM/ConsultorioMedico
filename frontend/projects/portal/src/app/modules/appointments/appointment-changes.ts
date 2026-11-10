import { Component, effect, inject, input, signal } from '@angular/core';
import { AppointmentChangeDto, AppointmentStatus, AppointmentsService, errorMessage } from 'sdk';
import { FechaPipe, UiBadge } from 'ui';
import { firstValueFrom } from 'rxjs';
import { appointmentStatusLabels } from './models/appointment-status-labels';

// The change trail of one appointment, read only. The API decides who may read it; nobody writes from the portal.
@Component({
  selector: 'appointment-changes',
  imports: [FechaPipe, UiBadge],
  template: `
    <h3 class="mt-6 text-sm font-bold">Movimientos</h3>
    @if (loading()) {
      <p class="mt-2 text-sm text-ink/60">Cargando movimientos…</p>
    } @else if (error()) {
      <p class="mt-2 text-sm text-pulse" role="alert">{{ error() }}</p>
    } @else if (!changes().length) {
      <p class="mt-2 text-sm text-ink/70">Este turno todavía no tiene movimientos registrados.</p>
    } @else {
      <ol class="mt-3 space-y-3 border-l-2 border-rule pl-4">
        @for (change of changes(); track change.id; let index = $index) {
          <li class="text-sm">
            <div class="flex flex-wrap items-center gap-2">
              <ui-badge [tone]="tone(change.status)">{{ labels[change.status] }}</ui-badge>
              <span class="font-semibold">{{ stepLabel(change, index) }}</span>
            </div>
            @if (movedSlot(change, index)) {
              <p class="mt-1 text-ink/70">Turno: {{ change.start | fecha }} con {{ change.professional }}</p>
            }
            <p class="mt-1 text-xs text-ink/60">{{ change.at | fecha }} · {{ change.by || 'Sistema' }}</p>
          </li>
        }
      </ol>
    }
  `,
})
export class AppointmentChanges {
  private readonly api = inject(AppointmentsService);
  readonly appointmentId = input.required<string>();
  readonly labels = appointmentStatusLabels;
  readonly changes = signal<AppointmentChangeDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor() {
    effect(() => void this.load(this.appointmentId()));
  }

  tone(status: AppointmentStatus): 'ok' | 'warn' | 'muted' {
    if (status === AppointmentStatus.Cancelled || status === AppointmentStatus.NoShow) return 'warn';
    return status === AppointmentStatus.Completed ? 'ok' : 'muted';
  }

  // A step that only moved the slot reads as a move, not as the same status twice.
  stepLabel(change: AppointmentChangeDto, index: number) {
    const previous = index > 0 ? this.changes()[index - 1] : null;
    if (!previous) return `Turno dado como ${appointmentStatusLabels[change.status].toLowerCase()}`;
    if (previous.status === change.status) return 'Cambio de horario o profesional';
    return `Pasó a ${appointmentStatusLabels[change.status].toLowerCase()}`;
  }

  movedSlot(change: AppointmentChangeDto, index: number) {
    const previous = index > 0 ? this.changes()[index - 1] : null;
    return !previous || previous.start !== change.start || previous.professional !== change.professional;
  }

  private async load(id: string) {
    this.loading.set(true);
    this.error.set('');
    this.changes.set([]);
    try {
      this.changes.set(await firstValueFrom(this.api.appointmentChanges(id)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
