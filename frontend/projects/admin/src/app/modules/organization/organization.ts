import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MedicalServiceDto, OrganizationService, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { Observable, firstValueFrom } from 'rxjs';
import { isOrganizationSection, organizationSections } from './models/section';

@Component({
  imports: [FormsModule, ScreenNote, SortCol, TablePager, UiButton, UiEmpty, UiField, UiModal, UiSkeleton, UiTable],
  templateUrl: './organization.html',
})
export class OrganizationPage {
  private readonly organization = inject(OrganizationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  private readonly params = toSignal(this.route.paramMap);
  readonly section = computed(() => {
    const value = this.params()?.get('section') ?? null;
    return isOrganizationSection(value) ? value : 'servicios';
  });
  readonly sectionLabel = computed(() => organizationSections.find((item) => item.id === this.section())?.label ?? 'Servicios');
  readonly services = signal<MedicalServiceDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly creating = signal(false);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
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
        firstValueFrom(this.organization.medicalServicesPage(listParams(this.page(), this.sort(), this.query()))),
      ]);
      this.services.set(services.items);
      this.total.set(services.total);
      this.page.set(services.page);
      this.locationId ||= locations[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  sortBy(key: string) {
    this.sort.update((current) => nextSort(current, key));
    this.page.set(1);
    void this.load();
  }

  onSearch(value: string) {
    this.query.set(value);
    this.page.set(1);
    queueSearch(this.searchHost, () => void this.load());
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  async createService() {
    await this.save(() => this.organization.createMedicalService({ locationId: this.locationId, name: this.serviceName }));
    if (!this.error()) {
      this.serviceName = '';
      this.creating.set(false);
    }
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
