import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppointmentTypeDto, LocationDto, MedicalServiceDto, OrganizationService, SpecialtyDto, errorMessage } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { OrganizationSection } from './models/section';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './organization.html',
})
export class OrganizationPage {
  private readonly organization = inject(OrganizationService);
  readonly sections: { id: OrganizationSection; label: string }[] = [
    { id: 'locations', label: 'Sedes' },
    { id: 'services', label: 'Servicios' },
    { id: 'specialties', label: 'Especialidades' },
    { id: 'appointmentTypes', label: 'Tipos de turno' },
  ];
  readonly section = signal<OrganizationSection>('locations');
  readonly locations = signal<LocationDto[]>([]);
  readonly services = signal<MedicalServiceDto[]>([]);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly appointmentTypes = signal<AppointmentTypeDto[]>([]);
  readonly error = signal('');
  locationName = '';
  address = '';
  locationId = '';
  serviceName = '';
  specialtyName = '';
  typeName = '';
  duration = 30;
  specialtyId = '';

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const [locations, services, specialties, appointmentTypes] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.medicalServices()),
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.organization.appointmentTypes()),
      ]);
      this.locations.set(locations);
      this.services.set(services);
      this.specialties.set(specialties);
      this.appointmentTypes.set(appointmentTypes);
      this.locationId ||= locations[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  locationNameOf(id: string) {
    return this.locations().find((location) => location.id === id)?.name ?? '';
  }

  async createLocation() {
    await this.save(() => this.organization.createLocation({ name: this.locationName, address: this.address }));
    this.locationName = '';
    this.address = '';
  }

  async createService() {
    await this.save(() => this.organization.createMedicalService({ locationId: this.locationId, name: this.serviceName }));
    this.serviceName = '';
  }

  async createSpecialty() {
    await this.save(() => this.organization.createSpecialty({ name: this.specialtyName }));
    this.specialtyName = '';
  }

  async createAppointmentType() {
    await this.save(() =>
      this.organization.createAppointmentType({
        name: this.typeName,
        durationMinutes: Number(this.duration),
        specialtyId: this.specialtyId || null,
      }),
    );
    this.typeName = '';
  }

  private async save(action: () => Observable<unknown>) {
    this.error.set('');
    try {
      await firstValueFrom(action());
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
