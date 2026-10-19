import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  AppointmentKind,
  BillingService,
  EmployeesService,
  OrganizationService,
  PrestacionDto,
  ProfessionalDto,
  SpecialtyDto,
  errorMessage,
} from 'sdk';
import { SelectOption, UiActions, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable } from 'ui';
import { ScreenNote } from '../../screen-note';
import { firstValueFrom } from 'rxjs';

type Dialog = 'duration' | 'price' | 'service';

@Component({
  imports: [FormsModule, RouterLink, ScreenNote, UiActions, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './specialty-detail.html',
  styleUrl: './specialty-detail.css',
})
export class SpecialtyDetailPage {
  private readonly organization = inject(OrganizationService);
  private readonly employees = inject(EmployeesService);
  private readonly billing = inject(BillingService);
  private readonly params = toSignal(inject(ActivatedRoute).paramMap);
  private request = 0;
  readonly specialty = signal<SpecialtyDto | null>(null);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly professionals = signal<ProfessionalDto[]>([]);
  readonly prestaciones = signal<PrestacionDto[]>([]);
  readonly insurers = signal<string[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly missing = signal(false);
  readonly busy = signal('');
  readonly saving = signal(false);
  readonly personId = signal('');
  readonly dialog = signal<Dialog | null>(null);
  readonly selected = signal<PrestacionDto | null>(null);
  readonly payer = signal('');
  readonly insurerMode = signal(false);
  readonly durationDraft = signal(30);
  readonly amountDraft = signal<number | null>(null);
  readonly serviceName = signal('');
  readonly priceError = computed(() => priceError(this.amountDraft(), this.currentAmount()));

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

  money(amount: number | null) {
    return amount == null ? 'Sin precio' : `$${amount}`;
  }

  currentAmount() {
    const item = this.selected();
    const payer = this.payer().trim();
    if (!item || (this.insurerMode() && !payer)) return null;
    if (!payer) return item.particular;
    return item.insurers.find((row) => row.payer.toLowerCase() === payer.toLowerCase())?.amount ?? null;
  }

  openDuration(item: PrestacionDto) {
    this.error.set('');
    this.selected.set(item);
    this.durationDraft.set(item.durationMinutes);
    this.dialog.set('duration');
  }

  openPrice(item: PrestacionDto, payer = '', insurer = false) {
    this.error.set('');
    this.selected.set(item);
    this.insurerMode.set(insurer || !!payer);
    this.payer.set(payer);
    const current = payer
      ? item.insurers.find((row) => row.payer.toLowerCase() === payer.toLowerCase())?.amount ?? null
      : insurer
        ? null
        : item.particular;
    this.amountDraft.set(current);
    this.dialog.set('price');
  }

  openService() {
    this.error.set('');
    this.serviceName.set('');
    this.durationDraft.set(30);
    this.dialog.set('service');
  }

  close() {
    if (this.saving()) return;
    this.dialog.set(null);
    this.error.set('');
  }

  onAmount(value: number | string | null) {
    if (value === null || value === '') {
      this.amountDraft.set(null);
      return;
    }
    const amount = typeof value === 'number' ? value : Number(value);
    this.amountDraft.set(Number.isNaN(amount) ? null : amount);
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

  async createDefault() {
    const specialty = this.specialty();
    if (!specialty) return;
    await this.run('default', async () => {
      await firstValueFrom(
        this.organization.createAppointmentType({
          name: specialty.name,
          durationMinutes: 30,
          specialtyId: specialty.id,
          kind: AppointmentKind.Consulta,
        }),
      );
    });
  }

  async saveDuration() {
    const specialty = this.specialty();
    const item = this.selected();
    const minutes = this.durationDraft();
    if (!specialty || !item) return;
    if (minutes < 5 || minutes > 240) {
      this.error.set('La duración debe estar entre 5 y 240 minutos.');
      return;
    }
    await this.saveDialog(async () => {
      await firstValueFrom(
        this.organization.updateAppointmentType(item.id, {
          name: item.name,
          durationMinutes: minutes,
          specialtyId: specialty.id,
          kind: item.kind,
        }),
      );
    });
  }

  async savePrice() {
    const item = this.selected();
    const amount = this.amountDraft();
    const message = this.insurerMode() && !this.payer().trim() ? 'La obra social necesita un nombre.' : priceError(amount, this.currentAmount());
    if (!item || amount === null || message) {
      this.error.set(message);
      return;
    }
    await this.saveDialog(async () => {
      await firstValueFrom(
        this.billing.createFee({
          appointmentTypeId: item.id,
          amount,
          effectiveFrom: new Date().toISOString().slice(0, 10),
          payer: this.payer().trim() || null,
        }),
      );
    });
  }

  async saveService() {
    const specialty = this.specialty();
    const name = this.serviceName().trim();
    const minutes = this.durationDraft();
    if (!specialty) return;
    if (!name) {
      this.error.set('La prestación necesita un nombre.');
      return;
    }
    if (minutes < 5 || minutes > 240) {
      this.error.set('La duración debe estar entre 5 y 240 minutos.');
      return;
    }
    await this.saveDialog(async () => {
      await firstValueFrom(
        this.organization.createAppointmentType({
          name,
          durationMinutes: minutes,
          specialtyId: specialty.id,
          kind: AppointmentKind.Estudio,
        }),
      );
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
      const [specialties, professionals, prestaciones] = await Promise.all([
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.employees.professionals()),
        firstValueFrom(this.organization.prestaciones(id)),
      ]);
      if (ticket !== this.request) return;
      const specialty = specialties.find((item) => item.id === id) ?? null;
      this.specialties.set(specialties);
      this.specialty.set(specialty);
      this.missing.set(!specialty);
      this.professionals.set(professionals);
      this.prestaciones.set(prestaciones.items);
      this.insurers.set(prestaciones.insurers);
    } catch (error) {
      if (ticket !== this.request) return;
      this.error.set(errorMessage(error));
    } finally {
      if (ticket === this.request) this.loading.set(false);
    }
  }

  private async saveDialog(action: () => Promise<void>) {
    const id = this.specialty()?.id;
    if (!id) return;
    this.error.set('');
    this.saving.set(true);
    try {
      await action();
      this.dialog.set(null);
      await this.load(id);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
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

function priceError(amount: number | null, current: number | null) {
  if (amount === null || Number.isNaN(amount)) return 'Escribí un importe.';
  const rounded = Math.round(amount * 100) / 100;
  if (rounded < 0) return 'El valor no puede ser negativo.';
  if (rounded === 0) return 'El valor no puede ser 0.';
  if (current !== null && rounded === Math.round(current * 100) / 100) return 'El valor tiene que ser distinto al actual.';
  return '';
}
