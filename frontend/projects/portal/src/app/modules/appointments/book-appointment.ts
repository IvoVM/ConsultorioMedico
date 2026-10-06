import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AppointmentsService,
  LocationDto,
  OrganizationService,
  ScheduleService,
  SlotDto,
  SpecialtyDto,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { UiButton, UiCalendar, UiEmpty, UiField, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiCalendar, UiEmpty, UiSkeleton],
  templateUrl: './book-appointment.html',
})
export class BookAppointmentPage {
  private readonly organization = inject(OrganizationService);
  private readonly schedule = inject(ScheduleService);
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly router = inject(Router);
  readonly session = clinicaSession;
  readonly locations = signal<LocationDto[]>([]);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly searching = signal(false);
  readonly joining = signal(false);
  readonly booking = signal('');
  locationId = '';
  specialtyId = '';
  date = new Date().toISOString().slice(0, 10);
  notes = '';

  constructor() {
    void this.prepare();
  }

  async prepare() {
    try {
      const [locations, specialties] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.specialties()),
      ]);
      this.locations.set(locations);
      this.specialties.set(specialties);
      this.locationId ||= locations[0]?.id ?? '';
      this.specialtyId ||= specialties[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async search() {
    this.error.set('');
    this.searching.set(true);
    try {
      this.slots.set(
        await firstValueFrom(
          this.schedule.availability({ locationId: this.locationId, specialtyId: this.specialtyId, date: this.date }),
        ),
      );
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.searching.set(false);
    }
  }

  async book(slot: SlotDto) {
    if (clinicaSession.role() !== 'Patient') {
      await this.router.navigateByUrl('/ingreso');
      return;
    }
    this.booking.set(slot.start + slot.professionalId);
    try {
      await firstValueFrom(
        this.appointmentsApi.bookAppointment({
          professionalId: slot.professionalId,
          locationId: slot.locationId,
          appointmentTypeId: slot.appointmentTypeId,
          start: slot.start,
          patientId: null,
          visitReason: null,
        }),
      );
      await this.router.navigateByUrl('/mis-turnos');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.booking.set('');
    }
  }

  async joinWaitlist() {
    this.joining.set(true);
    try {
      await firstValueFrom(
        this.appointmentsApi.joinWaitlist({
          patientId: null,
          professionalId: null,
          locationId: this.locationId,
          specialtyId: this.specialtyId,
          notes: this.notes,
        }),
      );
      this.error.set('Quedaste en la lista de espera.');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.joining.set(false);
    }
  }
}
