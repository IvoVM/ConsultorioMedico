import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentsService, BookingRequestDto, BookingRequestStatus, SlotDto, errorMessage } from 'sdk';
import { FechaPipe, ToastService, UiActions, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, FechaPipe, UiActions, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './requests.html',
})
export class AppointmentRequestsPage {
  private readonly appointments = inject(AppointmentsService);
  private readonly toasts = inject(ToastService);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly moving = signal<BookingRequestDto | null>(null);
  readonly denying = signal<BookingRequestDto | null>(null);
  readonly error = signal('');
  readonly slotsError = signal('');
  readonly loading = signal(true);
  readonly pending = signal('');
  readonly slotsLoading = signal(false);
  moveDate = new Date().toISOString().slice(0, 10);
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
      await firstValueFrom(this.appointments.approveRequest(item.id));
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
      await firstValueFrom(this.appointments.rejectRequest(item.id, { reason: this.reason.trim() }));
      this.toasts.success('Solicitud denegada', `La solicitud de ${item.name} quedó denegada.`);
      this.denying.set(null);
      await this.load();
    } catch (error) {
      this.toasts.error('No se pudo denegar', errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  async startMove(item: BookingRequestDto) {
    this.moving.set(item);
    this.moveDate = item.start.slice(0, 10);
    this.slots.set([]);
    this.slotsError.set('');
    await this.loadSlots();
  }

  cancelMove() {
    this.moving.set(null);
    this.slots.set([]);
    this.slotsError.set('');
  }

  async loadSlots() {
    const item = this.moving();
    if (!item || !this.moveDate) return;
    this.slotsLoading.set(true);
    this.slotsError.set('');
    try {
      this.slots.set(await firstValueFrom(this.appointments.requestSlots(item.id, this.moveDate)));
    } catch (error) {
      this.slots.set([]);
      this.slotsError.set(errorMessage(error));
    } finally {
      this.slotsLoading.set(false);
    }
  }

  async moveTo(slot: SlotDto) {
    const item = this.moving();
    if (!item) return;
    this.pending.set(`move:${item.id}`);
    try {
      await firstValueFrom(this.appointments.rescheduleRequest(item.id, { start: slot.start }));
      this.toasts.success('Turno reprogramado', `${item.name} quedó para el ${formatFecha(slot.start)}.`);
      this.cancelMove();
      await this.load();
    } catch (error) {
      this.toasts.error('No se pudo reprogramar', errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }

  private async load() {
    try {
      this.requests.set(await firstValueFrom(this.appointments.pendingRequests()));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
