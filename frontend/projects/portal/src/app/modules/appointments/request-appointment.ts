import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AppointmentsService,
  ClinicalService,
  InsurerDto,
  OrganizationService,
  ScheduleService,
  SlotDto,
  SpecialtyDto,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { UiButton, UiEmpty, UiField, UiSkeleton, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';

type Step = 'specialty' | 'day' | 'time' | 'name' | 'phone' | 'coverage' | 'insurer' | 'member' | 'review' | 'done';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiSkeleton],
  templateUrl: './request-appointment.html',
  styleUrl: './request-appointment.css',
})
export class RequestAppointmentPage {
  private readonly organization = inject(OrganizationService);
  private readonly schedule = inject(ScheduleService);
  private readonly appointments = inject(AppointmentsService);
  private readonly clinical = inject(ClinicalService);
  readonly session = clinicaSession;
  readonly step = signal<Step>('specialty');
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly insurers = signal<InsurerDto[]>([]);
  readonly openDays = signal<string[]>([]);
  readonly slots = signal<SlotDto[]>([]);
  readonly slot = signal<SlotDto | null>(null);
  readonly loading = signal(true);
  readonly sending = signal(false);
  readonly error = signal('');
  readonly locationIds = signal<string[]>([]);
  readonly specialtyId = signal('');
  readonly year = signal(new Date().getFullYear());
  readonly month = signal(new Date().getMonth() + 1);
  coverage: 'particular' | 'obra' = 'particular';
  insurerId = '';
  memberNumber = '';
  name = '';
  phone = '';

  readonly specialtyName = computed(() => this.specialties().find((item) => item.id === this.specialtyId())?.name ?? '');
  readonly steps = computed(() => {
    const list: Step[] = ['specialty', 'day', 'time', 'name', 'phone', 'coverage'];
    if (this.coverage === 'obra' && this.insurers().length) list.push('insurer', 'member');
    list.push('review');
    return list;
  });
  readonly stepIndex = computed(() => this.steps().indexOf(this.step()) + 1);
  readonly monthTitle = computed(() => {
    const label = new Date(this.year(), this.month() - 1, 1).toLocaleDateString('es-AR', { month: 'long' });
    return `${label.charAt(0).toUpperCase()}${label.slice(1)} ${this.year()}`;
  });
  readonly cells = computed(() => {
    const first = new Date(this.year(), this.month() - 1, 1);
    const lead = (first.getDay() + 6) % 7;
    const count = new Date(this.year(), this.month(), 0).getDate();
    const open = new Set(this.openDays().map((day) => day.slice(0, 10)));
    const cells: Array<{ iso: string; day: number; open: boolean } | null> = Array.from({ length: lead }, () => null);
    for (let day = 1; day <= count; day++) {
      const iso = `${this.year()}-${String(this.month()).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      cells.push({ iso, day, open: open.has(iso) });
    }
    return cells;
  });
  readonly canGoBackMonth = computed(() => {
    const today = new Date();
    return this.year() > today.getFullYear() || (this.year() === today.getFullYear() && this.month() > today.getMonth() + 1);
  });

  constructor() {
    void this.start();
  }

  question() {
    switch (this.step()) {
      case 'specialty': return '¿Para qué especialidad?';
      case 'day': return '¿Qué día te queda bien?';
      case 'time': return '¿A qué hora?';
      case 'name': return '¿Cómo te llamás?';
      case 'phone': return '¿A qué número te llamamos?';
      case 'coverage': return '¿Tenés obra social?';
      case 'insurer': return '¿Cuál es tu obra social?';
      case 'member': return '¿Cuál es tu número de afiliado?';
      case 'review': return 'Revisá la solicitud';
      default: return 'Listo';
    }
  }

  hint() {
    switch (this.step()) {
      case 'specialty': return 'Es el primer dato. Después elegís el día.';
      case 'day': return 'Los días marcados tienen un horario libre.';
      case 'time': return `${this.specialtyName()}. Elegí un horario.`;
      case 'name': return 'Nombre y apellido, como figuran en el documento.';
      case 'phone': return 'La secretaría te llama a este número si hace falta.';
      case 'coverage': return this.insurers().length ? 'Si no tenés, el turno queda como particular.' : 'Este profesional atiende como particular.';
      case 'insurer': return 'Solo las que atiende el profesional de ese horario.';
      case 'member': return 'La secretaría lo revisa antes de confirmar.';
      case 'review': return 'Al enviarla, la secretaría confirma el turno.';
      default: return 'La secretaría va a revisar tu solicitud y confirmar el turno.';
    }
  }

  when(value: string) {
    return formatFecha(value);
  }

  time(value: string) {
    return formatFecha(value, 'hora');
  }

  back() {
    const list = this.steps();
    const index = list.indexOf(this.step());
    if (index > 0) this.step.set(list[index - 1]);
    this.error.set('');
  }

  async pickSpecialty(id: string) {
    this.specialtyId.set(id);
    this.error.set('');
    const today = new Date();
    this.year.set(today.getFullYear());
    this.month.set(today.getMonth() + 1);
    this.step.set('day');
    await this.loadMonth();
  }

  async shiftMonth(delta: number) {
    const next = new Date(this.year(), this.month() - 1 + delta, 1);
    const today = new Date();
    if (next.getFullYear() < today.getFullYear() || (next.getFullYear() === today.getFullYear() && next.getMonth() < today.getMonth())) return;
    this.year.set(next.getFullYear());
    this.month.set(next.getMonth() + 1);
    await this.loadMonth();
  }

  async pickDay(iso: string) {
    this.error.set('');
    this.loading.set(true);
    try {
      const lists = await Promise.all(this.locationIds().map((locationId) => firstValueFrom(this.schedule.availability({
        locationId,
        specialtyId: this.specialtyId(),
        date: iso,
      }))));
      const now = Date.now();
      this.slots.set(lists.flat().filter((slot) => new Date(slot.start).getTime() > now).sort((left, right) => left.start.localeCompare(right.start)));
      this.step.set('time');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async pickSlot(slot: SlotDto) {
    this.slot.set(slot);
    this.coverage = 'particular';
    this.insurerId = '';
    this.memberNumber = '';
    this.error.set('');
    try {
      this.insurers.set(await firstValueFrom(this.organization.professionalInsurers(slot.professionalId)));
    } catch {
      this.insurers.set([]);
    }
    this.step.set('name');
  }

  continueName() {
    const name = this.name.trim();
    if (name.length < 3 || !name.includes(' ')) {
      this.error.set('Escribí tu nombre y tu apellido.');
      return;
    }
    this.name = name;
    this.error.set('');
    this.step.set('phone');
  }

  continuePhone() {
    const phone = this.phone.trim();
    if (phone.replace(/\D/g, '').length < 6) {
      this.error.set('El teléfono no es válido.');
      return;
    }
    this.phone = phone;
    this.error.set('');
    this.step.set('coverage');
  }

  pickCoverage(kind: 'particular' | 'obra') {
    this.coverage = kind;
    this.error.set('');
    if (kind === 'particular' || !this.insurers().length) {
      this.coverage = 'particular';
      this.insurerId = '';
      this.memberNumber = '';
      this.step.set('review');
      return;
    }
    this.step.set('insurer');
  }

  pickInsurer(id: string) {
    this.insurerId = id;
    this.error.set('');
    this.step.set('member');
  }

  continueMember() {
    if (!this.memberNumber.trim()) {
      this.error.set('El número de afiliado es obligatorio.');
      return;
    }
    this.memberNumber = this.memberNumber.trim();
    this.error.set('');
    this.step.set('review');
  }

  insurerName() {
    return this.insurers().find((item) => item.id === this.insurerId)?.name ?? '';
  }

  async send() {
    const slot = this.slot();
    if (!slot) return;
    this.sending.set(true);
    this.error.set('');
    try {
      await firstValueFrom(this.appointments.createRequest({
        specialtyId: this.specialtyId(),
        professionalId: slot.professionalId,
        locationId: slot.locationId,
        appointmentTypeId: slot.appointmentTypeId,
        start: slot.start,
        name: this.name.trim(),
        phone: this.phone.trim(),
        insurerId: this.coverage === 'obra' ? this.insurerId : null,
        memberNumber: this.coverage === 'obra' ? this.memberNumber.trim() : null,
      }));
      this.step.set('done');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.sending.set(false);
    }
  }

  private async start() {
    try {
      const [locations, specialties] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.specialties()),
      ]);
      this.specialties.set(specialties);
      this.locationIds.set(locations.filter((location) => location.isActive).map((location) => location.id));
      if (clinicaSession.role() === 'Patient') {
        const history = await firstValueFrom(this.clinical.clinicalHistory()).catch(() => null);
        if (history) {
          this.name = history.patient.name;
          this.phone = history.patient.phone;
        }
      }
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadMonth() {
    this.loading.set(true);
    this.error.set('');
    try {
      const days = await firstValueFrom(this.schedule.openDays({
        specialtyId: this.specialtyId(),
        year: this.year(),
        month: this.month(),
      }));
      this.openDays.set(days);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
