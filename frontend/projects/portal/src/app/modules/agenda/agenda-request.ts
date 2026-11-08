import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, firstValueFrom } from 'rxjs';
import { AppointmentsService, BookingRequestDto, BookingRequestStatus, errorMessage } from 'sdk';
import { RescheduleSource, ToastService, UiButton, UiField, UiModal, UiReschedule, formatFecha, overdueLabel, requestExpired } from 'ui';

enum RequestMode {
  Info = 'info',
  Move = 'move',
  Deny = 'deny',
}

@Component({
  selector: 'agenda-request',
  imports: [FormsModule, UiButton, UiField, UiModal, UiReschedule],
  templateUrl: './agenda-request.html',
})
export class AgendaRequest {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly toasts = inject(ToastService);

  readonly request = input.required<BookingRequestDto>();
  readonly changed = output<void>();
  readonly closed = output<void>();

  readonly modes = RequestMode;
  readonly mode = signal(RequestMode.Info);
  readonly saving = signal(false);
  readonly expired = computed(() => requestExpired(this.request().start));
  readonly overdue = computed(() => overdueLabel(this.request().start));
  readonly statusText = computed(() => (this.request().status === BookingRequestStatus.Rescheduled ? 'Reprogramada' : 'Pendiente'));
  readonly when = formatFecha;
  reason = '';
  source: RescheduleSource | null = null;

  async approve() {
    const item = this.request();
    if (!(await this.run(this.appointmentsApi.approveRequest(item.id), 'No se pudo aprobar'))) return;
    this.toasts.success('Turno aprobado', `${item.name} quedó el ${formatFecha(item.start)} con ${item.professional}.`);
    this.changed.emit();
  }

  startMove() {
    const item = this.request();
    this.source = {
      days: (year, month) => this.call(this.appointmentsApi.requestDays(item.id, year, month)),
      slots: (date) => this.call(this.appointmentsApi.requestSlots(item.id, date)),
      nearest: () => this.call(this.appointmentsApi.nearestRequestSlot(item.id)),
      confirm: async (start) => {
        await this.call(this.appointmentsApi.rescheduleRequest(item.id, { start }));
        this.toasts.success('Turno reprogramado', `${item.name} quedó para el ${formatFecha(start)}.`);
        this.changed.emit();
      },
    };
    this.mode.set(RequestMode.Move);
  }

  startDeny() {
    this.reason = '';
    this.mode.set(RequestMode.Deny);
  }

  back() {
    this.source = null;
    this.mode.set(RequestMode.Info);
  }

  async deny() {
    const item = this.request();
    if (!(await this.run(this.appointmentsApi.rejectRequest(item.id, { reason: this.reason.trim() }), 'No se pudo denegar'))) return;
    this.toasts.success('Solicitud denegada', `La solicitud de ${item.name} quedó denegada.`);
    this.changed.emit();
  }

  private async run<T>(request: Observable<T>, failure: string) {
    this.saving.set(true);
    try {
      await firstValueFrom(request);
      return true;
    } catch (error) {
      this.toasts.error(failure, errorMessage(error));
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  private async call<T>(request: Observable<T>) {
    try {
      return await firstValueFrom(request);
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  }
}
