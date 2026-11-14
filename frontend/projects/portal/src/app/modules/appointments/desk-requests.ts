import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentsService, BookingRequestDto, BookingRequestStatus, errorMessage } from 'sdk';
import { FechaPipe, RequestTab, RescheduleSource, TabItem, ToastService, UiButton, UiEmpty, UiField, UiModal, UiReschedule, UiSkeleton, UiTable, UiTabs, UiTone, formatFecha, deactivationLabel, expiredRequestDays, overdueLabel, requestDeactivated, requestExpired, requestTabLabels, UiHelp } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';

@Component({
  imports: [UiHelp, FormsModule, FechaPipe, UiButton, UiEmpty, UiField, UiModal, UiReschedule, UiSkeleton, UiTable, UiTabs],
  templateUrl: './desk-requests.html',
})
export class DeskRequestsPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly toasts = inject(ToastService);
  readonly booking = BookingRequestStatus;
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly moving = signal<BookingRequestDto | null>(null);
  readonly denying = signal<BookingRequestDto | null>(null);
  readonly error = signal('');
  readonly pending = signal('');
  readonly loading = signal(true);
  readonly tabKeys = RequestTab;
  readonly overdueLabel = overdueLabel;
  readonly deactivationLabel = deactivationLabel;
  readonly expiredRequestDays = expiredRequestDays;
  readonly tab = signal<string>(RequestTab.Vigentes);
  readonly expired = computed(() => this.requests().filter((item) => requestExpired(item.start) && !requestDeactivated(item.start)));
  readonly current = computed(() => this.requests().filter((item) => !requestExpired(item.start)));
  readonly shown = computed(() => (this.tab() === RequestTab.Vencidas ? this.expired() : this.current()));
  readonly tabs = computed((): TabItem[] => [
    { key: RequestTab.Vigentes, label: requestTabLabels[RequestTab.Vigentes], icon: 'pending_actions', count: this.current().length, tone: UiTone.Warn },
    { key: RequestTab.Vencidas, label: requestTabLabels[RequestTab.Vencidas], icon: 'event_busy', count: this.expired().length, tone: UiTone.Bad },
  ]);
  moveSource: RescheduleSource | null = null;
  reason = '';

  constructor() {
    void this.load();
  }

  statusLabel(status: BookingRequestStatus) {
    return status === BookingRequestStatus.Rescheduled ? 'Reprogramada' : 'Pendiente';
  }

  async decide(item: BookingRequestDto, accepted: boolean) {
    if (!accepted) {
      this.denying.set(item);
      this.reason = '';
      return;
    }
    this.pending.set(`approve:${item.id}`);
    try {
      await firstValueFrom(this.appointmentsApi.approveRequest(item.id));
      this.toasts.success('Turno aprobado', `${item.name} quedó el ${formatFecha(item.start)} con ${item.professional}.`);
      await this.load();
    } catch (error) {
      this.toasts.error('No se pudo aprobar', errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  async confirmDeny() {
    const item = this.denying();
    if (!item) return;
    this.pending.set(`deny:${item.id}`);
    try {
      await firstValueFrom(this.appointmentsApi.rejectRequest(item.id, { reason: this.reason.trim() }));
      this.toasts.success('Solicitud denegada', `La solicitud de ${item.name} quedó denegada.`);
      this.denying.set(null);
      await this.load();
    } catch (error) {
      this.toasts.error('No se pudo denegar', errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  startMove(item: BookingRequestDto) {
    this.moveSource = {
      days: (year, month) => this.call(this.appointmentsApi.requestDays(item.id, year, month)),
      slots: (date) => this.call(this.appointmentsApi.requestSlots(item.id, date)),
      nearest: () => this.call(this.appointmentsApi.nearestRequestSlot(item.id)),
      confirm: async (start) => {
        await this.call(this.appointmentsApi.rescheduleRequest(item.id, { start }));
        this.toasts.success('Turno reprogramado', `${item.name} quedó para el ${formatFecha(start)}.`);
        this.cancelMove();
        await this.load();
      },
    };
    this.moving.set(item);
  }

  cancelMove() {
    this.moving.set(null);
    this.moveSource = null;
  }

  private async call<T>(request: Observable<T>) {
    try {
      return await firstValueFrom(request);
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  }

  private async load() {
    try {
      this.requests.set(await firstValueFrom(this.appointmentsApi.pendingRequests()));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
