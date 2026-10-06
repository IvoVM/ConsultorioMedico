import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AppointmentTypeDto, LocationDto, MedicalServiceDto, OrganizationService, SpecialtyDto, errorMessage } from 'sdk';
import { UiButton, UiField, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { isOrganizationSection, organizationSections } from './models/section';

@Component({
  imports: [FormsModule, UiButton, UiField, UiTable],
  templateUrl: './organization.html',
})
export class OrganizationPage {
  private readonly organization = inject(OrganizationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly params = toSignal(this.route.paramMap);
  readonly section = computed(() => {
    const value = this.params()?.get('section') ?? null;
    return isOrganizationSection(value) ? value : 'sedes';
  });
  readonly sectionLabel = computed(() => organizationSections.find((item) => item.id === this.section())?.label ?? 'Sedes');
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
    effect(() => {
      const value = this.params()?.get('section');
      if (value && !isOrganizationSection(value)) void this.router.navigate(['/organizacion/sedes'], { replaceUrl: true });
    });
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
