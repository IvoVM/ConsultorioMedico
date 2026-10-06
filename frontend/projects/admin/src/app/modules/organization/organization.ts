import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MedicalServiceDto, OrganizationService, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { isOrganizationSection, organizationSections } from './models/section';

@Component({
  imports: [FormsModule, UiButton, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './organization.html',
})
export class OrganizationPage {
  private readonly organization = inject(OrganizationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly params = toSignal(this.route.paramMap);
  readonly section = computed(() => {
    const value = this.params()?.get('section') ?? null;
    return isOrganizationSection(value) ? value : 'servicios';
  });
  readonly sectionLabel = computed(() => organizationSections.find((item) => item.id === this.section())?.label ?? 'Servicios');
  readonly services = signal<MedicalServiceDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  locationId = '';
  serviceName = '';

  constructor() {
    effect(() => {
      const value = this.params()?.get('section');
      if (value && !isOrganizationSection(value)) void this.router.navigate(['/organizacion/servicios'], { replaceUrl: true });
    });
    void this.load();
  }

  async load() {
    try {
      const [locations, services] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.medicalServices()),
      ]);
      this.services.set(services);
      this.locationId ||= locations[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async createService() {
    await this.save(() => this.organization.createMedicalService({ locationId: this.locationId, name: this.serviceName }));
    this.serviceName = '';
  }

  private async save(action: () => Observable<unknown>) {
    this.error.set('');
    this.saving.set(true);
    try {
      await firstValueFrom(action());
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
