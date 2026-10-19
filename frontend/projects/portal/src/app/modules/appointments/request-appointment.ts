import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AppointmentsService,
  ClinicalService,
  InsurerDto,
  OrganizationService,
  ScheduleService,
  AppointmentTypeDto,
  SlotDto,
  SpecialtyDto,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { UiButton, UiEmpty, UiField, UiModal, UiSkeleton, formatFecha } from 'ui';
import { firstValueFrom } from 'rxjs';

type Step = 'specialty' | 'day' | 'time' | 'data' | 'coverage' | 'review' | 'done';
type Intent = 'consulta' | 'estudio';

@Component({
  imports: [FormsModule, RouterLink, UiButton, UiEmpty, UiField, UiModal, UiSkeleton],
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
  readonly intent = signal<Intent | null>(null);
  readonly specialties = signal<SpecialtyDto[]>([]);
  readonly types = signal<AppointmentTypeDto[]>([]);
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
  readonly coverage = signal<'particular' | 'obra'>('particular');
  readonly offer = signal<'choose' | 'confirm' | null>(null);
  readonly nearest = signal<SlotDto | null>(null);
  readonly seeking = signal(false);
  readonly studyId = signal('');
  readonly visitTypeIds = signal<string[]>([]);
  insurerId = '';
  memberNumber = '';
  name = '';
  documentNumber = '';
  phone = '';

  readonly specialtyName = computed(() => {
    if (this.intent() === 'estudio') return this.types().find((type) => type.id === this.studyId())?.name ?? '';
    return this.specialties().find((item) => item.id === this.specialtyId())?.name ?? '';
  });
  readonly catalog = computed(() => {
    const types = this.types();
    if (this.intent() === 'estudio') {
      return types
        .filter((type) => type.specialtyId && !isVisit(type.name))
        .map((type) => ({ id: type.id, name: type.name, specialtyId: type.specialtyId ?? '' }))
        .sort((left, right) => left.name.localeCompare(right.name, 'es'));
    }
    const ids = new Set(types.filter((type) => type.specialtyId && isVisit(type.name)).map((type) => type.specialtyId));
    return this.specialties()
      .filter((item) => ids.has(item.id))
      .map((item) => ({ id: item.id, name: item.name, specialtyId: item.id }))
      .sort((left, right) => left.name.localeCompare(right.name, 'es'));
  });
  readonly furthest = signal<Step>('specialty');
  readonly direction = signal<1 | -1>(1);
  readonly motion = signal(0);
  readonly steps = computed((): Step[] => ['specialty', 'day', 'time', 'data', 'coverage', 'review']);
  readonly stepIndex = computed(() => this.steps().indexOf(this.step()));
  readonly furthestIndex = computed(() => this.steps().indexOf(this.furthest()));
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

  stepLabel(step: Step) {
    switch (step) {
      case 'specialty': return this.intent() === 'estudio' ? 'Estudio' : 'Consulta';
      case 'day': return 'Día';
      case 'time': return 'Hora';
      case 'data': return 'Tus datos';
      case 'coverage': return 'Cobertura';
      case 'review': return 'Confirmar';
      default: return '';
    }
  }

  question() {
    switch (this.step()) {
      case 'specialty': return this.intent() === 'estudio' ? '¿Qué estudio necesitás?' : '¿Qué consulta necesitás?';
      case 'day': return '¿Qué día te queda bien?';
      case 'time': return '¿A qué hora?';
      case 'data': return 'Tus datos';
      case 'coverage': return '¿Tenés obra social?';
      case 'review': return 'Revisá la solicitud';
      default: return 'Listo';
    }
  }

  hint() {
    switch (this.step()) {
      case 'specialty': return this.intent() === 'estudio'
        ? 'Es una práctica. Después podés tomar el turno más próximo o elegir el día.'
        : 'Es para ver a un profesional. Después podés tomar el turno más próximo o elegir el día.';
      case 'day': return 'Los días marcados tienen un horario libre.';
      case 'time': return `${this.specialtyName()}. Elegí un horario.`;
      case 'data': return 'Nombre, documento y teléfono. Con el documento vas a poder ver esta solicitud.';
      case 'coverage': return this.insurers().length ? 'Si no tenés, el turno queda como particular.' : 'Este profesional atiende como particular.';
      case 'review': return 'Al enviarla, la secretaría confirma el turno.';
      default: return this.session.token()
        ? 'La secretaría puede llamarte para confirmar el turno. También lo vas a ver en Mis turnos.'
        : 'La secretaría puede llamarte para confirmar el turno. Si querés verlo en el momento, creá una cuenta.';
    }
  }

  accountDraft() {
    const parts = this.name.trim().split(/\s+/);
    return {
      documento: this.documentNumber,
      nombre: parts[0] ?? '',
      apellido: parts.slice(1).join(' '),
      telefono: this.phone,
    };
  }

  when(value: string) {
    return formatFecha(value);
  }

  time(value: string) {
    return formatFecha(value, 'hora');
  }

  jump(step: Step) {
    if (this.steps().indexOf(step) > this.furthestIndex()) return;
    this.offer.set(null);
    this.error.set('');
    this.show(step);
    if (step === 'day') void this.loadMonth();
  }

  private go(next: Step) {
    const order = this.steps();
    if (order.indexOf(next) > order.indexOf(this.furthest())) this.furthest.set(next);
    this.error.set('');
    this.show(next);
  }

  private show(next: Step) {
    if (this.step() === next) return;
    const from = this.steps().indexOf(this.step());
    const to = next === 'done' ? from + 1 : this.steps().indexOf(next);
    this.direction.set(to >= from ? 1 : -1);
    this.step.set(next);
    this.motion.update((value) => value + 1);
  }

  backToIntent() {
    this.intent.set(null);
    this.step.set('specialty');
    this.furthest.set('specialty');
    this.offer.set(null);
    this.specialtyId.set('');
    this.studyId.set('');
    this.visitTypeIds.set([]);
    this.slot.set(null);
    this.error.set('');
  }

  pickIntent(kind: Intent) {
    this.intent.set(kind);
    this.step.set('specialty');
    this.furthest.set('specialty');
    this.offer.set(null);
    this.specialtyId.set('');
    this.studyId.set('');
    this.visitTypeIds.set([]);
    this.slot.set(null);
    this.error.set('');
  }

  pickSpecialty(option: { id: string; specialtyId: string }) {
    this.specialtyId.set(option.specialtyId);
    if (this.intent() === 'estudio') {
      this.studyId.set(option.id);
      this.visitTypeIds.set([]);
    } else {
      this.studyId.set('');
      this.visitTypeIds.set(this.types().filter((type) => type.specialtyId === option.specialtyId && isVisit(type.name)).map((type) => type.id));
    }
    this.error.set('');
    this.nearest.set(null);
    this.offer.set('choose');
  }

  async chooseNearest() {
    this.seeking.set(true);
    this.error.set('');
    try {
      const slot = await this.findNearest();
      if (!slot) {
        this.offer.set(null);
        this.error.set('No hay un turno próximo. Elegí el día y el horario.');
        await this.chooseManual();
        return;
      }
      this.nearest.set(slot);
      this.offer.set('confirm');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.seeking.set(false);
    }
  }

  async chooseManual() {
    this.offer.set(null);
    const today = new Date();
    this.year.set(today.getFullYear());
    this.month.set(today.getMonth() + 1);
    this.go('day');
    await this.loadMonth();
  }

  async acceptNearest() {
    const slot = this.nearest();
    if (!slot) return;
    this.offer.set(null);
    await this.applySlot(slot);
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
      this.slots.set(lists.flat().filter((slot) => new Date(slot.start).getTime() > now && this.accepts(slot)).sort((left, right) => left.start.localeCompare(right.start)));
      this.go('time');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  pickSlot(slot: SlotDto) {
    void this.applySlot(slot);
  }

  continueData() {
    const name = this.name.trim();
    const documentNumber = this.documentNumber.replace(/[.\-\s]/g, '');
    const phone = this.phone.trim();
    if (name.length < 3 || !name.includes(' ')) {
      this.error.set('Escribí tu nombre y tu apellido.');
      return;
    }
    if (documentNumber.length < 6 || documentNumber.length > 20) {
      this.error.set('El documento no es válido.');
      return;
    }
    if (phone.replace(/\D/g, '').length < 6) {
      this.error.set('El teléfono no es válido.');
      return;
    }
    this.name = name;
    this.documentNumber = documentNumber;
    this.phone = phone;
    this.error.set('');
    this.go('coverage');
  }

  pickCoverage(kind: 'particular' | 'obra') {
    this.error.set('');
    if (kind === 'particular' || !this.insurers().length) {
      this.coverage.set('particular');
      this.insurerId = '';
      this.memberNumber = '';
      this.go('review');
      return;
    }
    this.coverage.set('obra');
  }

  pickInsurer(id: string) {
    this.insurerId = id;
    this.error.set('');
  }

  continueCoverage() {
    if (!this.insurerId) {
      this.error.set('Elegí tu obra social.');
      return;
    }
    if (!this.memberNumber.trim()) {
      this.error.set('El número de afiliado es obligatorio.');
      return;
    }
    this.memberNumber = this.memberNumber.trim();
    this.error.set('');
    this.go('review');
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
        documentNumber: this.documentNumber.trim(),
        insurerId: this.coverage() === 'obra' ? this.insurerId : null,
        memberNumber: this.coverage() === 'obra' ? this.memberNumber.trim() : null,
      }));
      this.show('done');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.sending.set(false);
    }
  }

  private async start() {
    try {
      const [locations, specialties, types] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.organization.appointmentTypes()),
      ]);
      this.specialties.set(specialties);
      this.types.set(types);
      this.locationIds.set(locations.filter((location) => location.isActive).map((location) => location.id));
      if (clinicaSession.role() === 'Patient') {
        const history = await firstValueFrom(this.clinical.clinicalHistory()).catch(() => null);
        if (history) {
          this.name = history.patient.name;
          this.phone = history.patient.phone;
          this.documentNumber = history.patient.documentNumber;
        }
      }
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async applySlot(slot: SlotDto) {
    this.slot.set(slot);
    this.coverage.set('particular');
    this.insurerId = '';
    this.memberNumber = '';
    this.error.set('');
    try {
      this.insurers.set(await firstValueFrom(this.organization.professionalInsurers(slot.professionalId)));
    } catch {
      this.insurers.set([]);
    }
    this.go('data');
  }

  private accepts(slot: SlotDto) {
    if (this.intent() === 'estudio') return slot.appointmentTypeId === this.studyId();
    return this.visitTypeIds().includes(slot.appointmentTypeId);
  }

  private async findNearest() {
    const now = new Date();
    const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
    for (let offset = 0; offset < 4; offset++) {
      const cursor = new Date(now.getFullYear(), now.getMonth() + offset, 1);
      const days = await firstValueFrom(this.schedule.openDays({
        specialtyId: this.specialtyId(),
        year: cursor.getFullYear(),
        month: cursor.getMonth() + 1,
      }));
      const upcoming = days.map((day) => day.slice(0, 10)).filter((day) => day >= today).sort();
      for (const iso of upcoming) {
        const lists = await Promise.all(this.locationIds().map((locationId) => firstValueFrom(this.schedule.availability({
          locationId,
          specialtyId: this.specialtyId(),
          date: iso,
        }))));
        const slot = lists.flat()
          .filter((item) => new Date(item.start).getTime() > Date.now() && this.accepts(item))
          .sort((left, right) => left.start.localeCompare(right.start))[0];
        if (slot) return slot;
      }
    }
    return null;
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

function isVisit(name: string) {
  const value = name.trim().toLowerCase();
  return value.startsWith('consulta') || value === 'control';
}
