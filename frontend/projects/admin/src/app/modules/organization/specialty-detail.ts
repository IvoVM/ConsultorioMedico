import { Component, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EmployeesService, OrganizationService, ProfessionalDto, SpecialtyDto, errorMessage } from 'sdk';
import { SelectOption, UiButton, UiEmpty, UiField, UiSelect, UiSkeleton } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiSelect, UiSkeleton],
  templateUrl: './specialty-detail.html',
  styleUrl: './specialty-detail.css',
})
export class SpecialtyDetailPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly params = toSignal(inject(ActivatedRoute).paramMap);
  private request = 0;
  readonly specialty = signal<SpecialtyDto | null>(null);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly missing = signal(false);
  readonly busy = signal('');
  readonly personId = signal('');

  constructor() {
    effect(() => {
      const id = this.params()?.get('specialtyId');
      if (id) untracked(() => void this.load(id));
    });
  }

  people() {
    const id = this.specialty()?.id;
    return this.professionals().filter((person) => person.specialtyId === id);
  }

  availablePeople(): SelectOption[] {
    const id = this.specialty()?.id;
    return this.professionals()
      .filter((person) => person.specialtyId !== id)
      .map((person) => ({
        value: person.id,
        label: person.specialtyId
          ? `${person.lastName}, ${person.firstName} · ${this.specialtyName(person.specialtyId)}`
          : `${person.lastName}, ${person.firstName}`,
      }));
  }

  specialtyName(id: string) {
    return this.specialties().find((item) => item.id === id)?.name ?? '';
  }

  async addPerson() {
    const specialty = this.specialty();
    const personId = this.personId();
    if (!specialty || !personId) return;
    await this.run('person', async () => {
      await firstValueFrom(this.employees.assignSpecialty(personId, { specialtyId: specialty.id }));
      this.personId.set('');
    });
  }

  async removePerson(person: ProfessionalDto) {
    await this.run(`remove:${person.id}`, async () => {
      await firstValueFrom(this.employees.assignSpecialty(person.id, { specialtyId: null }));
    });
  }

  private async load(id: string) {
    const ticket = ++this.request;
    const switching = this.specialty()?.id !== id;
    if (switching) {
      this.specialty.set(null);
      this.loading.set(true);
    }
    this.error.set('');
    this.missing.set(false);
    try {
      const [specialties, professionals] = await Promise.all([
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
      ]);
      if (ticket !== this.request) return;
      const specialty = specialties.find((item) => item.id === id) ?? null;
      this.specialties.set(specialties);
      this.specialty.set(specialty);
      this.missing.set(!specialty);
      this.professionals.set(professionals);
    } catch (error) {
      if (ticket !== this.request) return;
      this.error.set(errorMessage(error));
    } finally {
      if (ticket === this.request) this.loading.set(false);
    }
  }

  private async run(key: string, action: () => Promise<void>) {
    const id = this.specialty()?.id;
    if (!id) return;
    this.error.set('');
    this.busy.set(key);
    try {
      await action();
      await this.load(id);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.busy.set('');
    }
  }
}
