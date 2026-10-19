import { Component, inject, signal } from '@angular/core';
import { AppointmentsService, BookingRequestDto, errorMessage } from 'sdk';
import { FechaPipe, UiActions, UiButton, UiEmpty, UiSkeleton, UiTable, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FechaPipe, UiActions, UiButton, UiEmpty, UiSkeleton, UiTable],
  templateUrl: './requests.html',
})
export class AppointmentRequestsPage {
  private readonly appointments = inject(AppointmentsService);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly error = signal('');
  readonly notice = signal('');
  readonly loading = signal(true);
  readonly pending = signal('');

  constructor() {
    void this.load();
  }

  async decide(item: BookingRequestDto, accepted: boolean) {
    this.pending.set(`${accepted ? 'approve' : 'reject'}:${item.id}`);
    this.error.set('');
    this.notice.set('');
    try {
      if (accepted) {
        await firstValueFrom(this.appointments.approveRequest(item.id));
        this.notice.set(`${item.name} quedó el ${formatFecha(item.start)} con ${item.professional}.`);
      } else {
        await firstValueFrom(this.appointments.rejectRequest(item.id));
        this.notice.set(`La solicitud de ${item.name} quedó denegada.`);
      }
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
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
