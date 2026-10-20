import { Component, inject, signal } from '@angular/core';
import { AppointmentsService, BookingRequestDto, errorMessage } from 'sdk';
import { FechaPipe, UiButton, UiEmpty, UiSkeleton, UiTable, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FechaPipe, UiButton, UiEmpty, UiSkeleton, UiTable],
  templateUrl: './desk-requests.html',
})
export class DeskRequestsPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly error = signal('');
  readonly notice = signal('');
  readonly pending = signal('');
  readonly loading = signal(true);

  constructor() {
    void this.load();
  }

  async decide(item: BookingRequestDto, accepted: boolean) {
    this.pending.set(`${accepted ? 'approve' : 'deny'}:${item.id}`);
    this.error.set('');
    this.notice.set('');
    try {
      if (accepted) {
        await firstValueFrom(this.appointmentsApi.approveRequest(item.id));
        this.notice.set(`${item.name} quedó el ${formatFecha(item.start)} con ${item.professional}.`);
      } else {
        await firstValueFrom(this.appointmentsApi.rejectRequest(item.id));
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
      this.requests.set(await firstValueFrom(this.appointmentsApi.pendingRequests()));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
