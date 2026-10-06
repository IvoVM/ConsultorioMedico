import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClinicaClient, LocationDto, SlotDto, SpecialtyDto, clinicaSession, errorMessage, tenantFromHost } from 'sdk';
import { UiButton, UiCalendar, UiEmpty, UiField } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, UiButton, UiField, UiCalendar, UiEmpty],
  templateUrl: './book-appointment.html',
})
export class BookAppointmentPage {
  private readonly api = inject(ClinicaClient);
  private readonly router = inject(Router);
  readonly session = clinicaSession;
  readonly locations = signal<LocationDto[]>([]);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly error = signal('');
  slug = tenantFromHost() ?? clinicaSession.tenant() ?? '';
  locationId = '';
  specialtyId = '';
  date = new Date().toISOString().slice(0, 10);
  notes = '';

  constructor() {
    if (this.slug) void this.prepare();
  }

  async prepare() {
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      const [locations, specialties] = await Promise.all([firstValueFrom(this.api.locations()), firstValueFrom(this.api.specialties())]);
      this.locations.set(locations);
      this.specialties.set(specialties);
      this.locationId ||= locations[0]?.id ?? '';
      this.specialtyId ||= specialties[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async search() {
    this.error.set('');
    clinicaSession.setTenant(this.slug.trim().toLowerCase());
    try {
      this.slots.set(
        await firstValueFrom(this.api.availability({ locationId: this.locationId, specialtyId: this.specialtyId, date: this.date })),
      );
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async book(slot: SlotDto) {
    if (clinicaSession.role() !== 'Patient') {
      await this.router.navigateByUrl('/ingreso');
      return;
    }
    try {
      await firstValueFrom(
        this.api.bookAppointment({
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
    }
  }

  async joinWaitlist() {
    try {
      await firstValueFrom(
        this.api.joinWaitlist({
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
    }
  }
}
