import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EmployeesService, OrganizationService, ProfessionalDto, SpecialtyDto, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, ScreenNote, UiButton, UiEmpty, UiField, UiSkeleton, UiTable],
  templateUrl: './specialties.html',
})
export class SpecialtiesPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly router = inject(Router);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly rows = computed(() =>
    this.specialties().map((item) => ({
      ...item,
      people: this.professionals().filter((person) => person.specialtyId === item.id).length,
    })),
  );
  specialtyName = '';

  constructor() {
    void this.load();
  }

  countLabel(count: number, one: string, many: string) {
    return count === 1 ? `1 ${one}` : `${count} ${many}`;
  }

  async createSpecialty() {
    this.error.set('');
    this.saving.set(true);
    try {
      const created = await firstValueFrom(this.organization.createSpecialty({ name: this.specialtyName.trim() }));
      await this.router.navigate(['/organizacion/especialidades', created.id]);
    } catch (error) {
      this.error.set(errorMessage(error));
      this.saving.set(false);
    }
  }

  private async load() {
    try {
      const [specialties, professionals] = await Promise.all([
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
      ]);
      this.specialties.set(specialties);
      this.professionals.set(professionals);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
