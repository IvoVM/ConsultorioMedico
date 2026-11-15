import { Component, inject, signal } from '@angular/core';
import { AppointmentsService, WaitlistEntryDto, errorMessage } from 'sdk';
import { ToastService, UiButton, UiEmpty, UiHelp, UiSkeleton, UiTable, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';

// Patients waiting for an earlier slot: front desk gives each one the next free slot that fits.
@Component({
  imports: [UiButton, UiEmpty, UiHelp, UiSkeleton, UiTable],
  templateUrl: './waitlist.html',
})
export class WaitlistPage {
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly toasts = inject(ToastService);
  readonly waitlist = signal<WaitlistEntryDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly pending = signal('');

  constructor() {
    void this.load();
  }

  async load() {
    try {
      this.waitlist.set(await firstValueFrom(this.appointmentsApi.waitlist()));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async offer(item: WaitlistEntryDto) {
    this.pending.set(item.id);
    try {
      const appointment = await firstValueFrom(this.appointmentsApi.offerWaitlist(item.id));
      this.toasts.success('Turno asignado', `${item.patient} tiene turno el ${formatFecha(appointment.start)} con ${appointment.professional}.`);
      await this.load();
    } catch (error) {
      this.toasts.error('No se pudo asignar el turno', errorMessage(error));
    } finally {
      this.pending.set('');
    }
  }
}
