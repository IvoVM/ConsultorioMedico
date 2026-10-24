import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AppointmentsService,
  ClinicalService,
  InsurerDto,
  OrganizationService,
  ScheduleService,
  AppointmentKind,
  TenantRole,
  AppointmentTypeDto,
  SlotDto,
  SpecialtyDto,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { UiButton, UiEmpty, UiField, UiModal, UiSkeleton, formatFecha, formatPhone } from 'ui';
import { firstValueFrom } from 'rxjs';
import { Coverage, Intent, Offer, Step } from './request-appointment.enums';
import { AppRoute } from '../../app.routes.enum';
import { AuthFrom } from '../auth/auth-from';


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
  readonly route = AppRoute;
  readonly Intent = Intent;
  readonly Coverage = Coverage;
  readonly Offer = Offer;
  readonly Step = Step;
  readonly step = signal<Step>(Step.Specialty);
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
  readonly coverage = signal<Coverage>(Coverage.Particular);
  readonly offer = signal<Offer | null>(null);
  readonly nearest = signal<SlotDto | null>(null);
  readonly seeking = signal(false);
  readonly studyId = signal('');
  readonly visitTypeIds = signal<string[]>([]);
  readonly dayIso = signal('');
  private readonly slotsFor = signal('');
  insurerId = '';
  memberNumber = '';
  firstName = '';
  lastName = '';
  documentNumber = '';
  phone = '';

  readonly specialtyName = computed(() => {
    if (this.intent() === Intent.Estudio) return this.types().find((type) => type.id === this.studyId())?.name ?? '';
    return this.specialties().find((item) => item.id === this.specialtyId())?.name ?? '';
  });
  readonly catalog = computed(() => {
    const types = this.types();
    if (this.intent() === Intent.Estudio) {
      return types
        .filter((type) => type.specialtyId && type.kind === AppointmentKind.Estudio)
        .map((type) => ({ id: type.id, name: type.name, specialtyId: type.specialtyId ?? '' }))
        .sort((left, right) => left.name.localeCompare(right.name, 'es'));
    }
    const ids = new Set(types.filter((type) => type.specialtyId && type.kind === AppointmentKind.Consulta).map((type) => type.specialtyId));
    return this.specialties()
      .filter((item) => ids.has(item.id))
      .map((item) => ({ id: item.id, name: item.name, specialtyId: item.id }))
      .sort((left, right) => left.name.localeCompare(right.name, 'es'));
  });
  readonly furthest = signal<Step>(Step.Specialty);
  readonly direction = signal<1 | -1>(1);
  readonly motion = signal(0);
  readonly steps = computed((): Step[] => [Step.Specialty, Step.Day, Step.Time, Step.Data, Step.Coverage, Step.Review]);
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
      case Step.Specialty: return this.intent() === Intent.Estudio ? 'Estudio' : 'Consulta';
      case Step.Day: return 'Día';
      case Step.Time: return 'Hora';
      case Step.Data: return 'Tus datos';
      case Step.Coverage: return 'Cobertura';
      case Step.Review: return 'Confirmar';
      default: return '';
    }
  }

  question() {
    switch (this.step()) {
      case Step.Specialty: return this.intent() === Intent.Estudio ? '¿Qué estudio necesitás?' : '¿Qué consulta necesitás?';
      case Step.Day: return '¿Qué día te queda bien?';
      case Step.Time: return '¿A qué hora?';
      case Step.Data: return 'Tus datos';
      case Step.Coverage: return '¿Tenés obra social?';
      case Step.Review: return 'Revisá la solicitud';
      default: return 'Listo';
    }
  }

  hint() {
    switch (this.step()) {
      case Step.Specialty: return this.intent() === Intent.Estudio
        ? 'Es una práctica. Después podés tomar el turno más próximo o elegir el día.'
        : 'Es para ver a un profesional. Después podés tomar el turno más próximo o elegir el día.';
      case Step.Day: return 'Solo aparecen los días con un horario libre.';
      case Step.Time: return `${this.specialtyName()}. Elegí un horario.`;
      case Step.Data: return 'Nombre, apellido, documento y teléfono. Con el documento vas a poder ver esta solicitud.';
      case Step.Coverage: return this.insurers().length ? 'Si no tenés, el turno queda como particular.' : 'Este profesional atiende como particular.';
      case Step.Review: return 'Al enviarla, la secretaría confirma el turno.';
      default: return this.session.token()
        ? 'La secretaría puede contactarte para confirmar el turno. También lo vas a ver en Mis turnos.'
        : 'La secretaría puede contactarte para confirmar el turno. Si querés ver el estado del turno y tu información personal, podés crear una cuenta.';
    }
  }

  accountDraft() {
    return {
      documento: this.documentNumber,
      nombre: this.firstName.trim(),
      apellido: this.lastName.trim(),
      telefono: this.phone,
      desde: AuthFrom.Solicitud,
    };
  }

  normalizePhone() {
    const formatted = formatPhone(this.phone);
    if (formatted) this.phone = formatted;
  }

  when(value: string) {
    return formatFecha(value);
  }

  time(value: string) {
    return formatFecha(value, 'hora');
  }

  canOpen(step: Step) {
    if (this.steps().indexOf(step) > this.furthestIndex()) return false;
    if (step === Step.Time) return this.dayIso() !== '';
    return true;
  }

  async jump(step: Step) {
    if (!this.canOpen(step)) return;
    this.offer.set(null);
    this.error.set('');
    if (step === Step.Time && this.slotsFor() !== this.dayIso()) await this.loadSlots(this.dayIso(), true);
    this.show(step);
    if (step === Step.Day) void this.loadMonth();
  }

  sheetRows() {
    const rows: Array<{ label: string; value: string }> = [];
    const kind = this.intent();
    if (!kind) return rows;
    rows.push({ label: 'Servicio elegido', value: kind === Intent.Estudio ? 'Estudio' : 'Consulta médica' });
    const service = this.specialtyName();
    if (service) rows.push({ label: kind === Intent.Estudio ? 'Estudio' : 'Consulta', value: service });
    if (this.dayIso()) rows.push({ label: 'Día', value: this.dayLabel(this.dayIso()) });
    const chosen = this.slot();
    if (chosen) {
      rows.push({ label: 'Hora', value: this.time(chosen.start) });
      rows.push({ label: 'Profesional', value: chosen.professional });
    }
    if (this.firstName.trim()) rows.push({ label: 'Nombre', value: this.firstName.trim() });
    if (this.lastName.trim()) rows.push({ label: 'Apellido', value: this.lastName.trim() });
    if (this.documentNumber.trim()) rows.push({ label: 'Documento', value: this.documentNumber.trim() });
    if (this.phone.trim()) rows.push({ label: 'Teléfono', value: this.phone.trim() });
    if (this.steps().indexOf(this.furthest()) >= this.steps().indexOf(Step.Review)) {
      rows.push({
        label: 'Cobertura',
        value: this.coverage() === Coverage.Obra ? `${this.insurerName()} · ${this.memberNumber}` : 'Particular',
      });
    }
    return rows;
  }

  private dayLabel(iso: string) {
    const [year, month, day] = iso.split('-').map(Number);
    const label = new Date(year, month - 1, day).toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
    return `${label.charAt(0).toUpperCase()}${label.slice(1)}`;
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
    const to = next === Step.Done ? from + 1 : this.steps().indexOf(next);
    this.direction.set(to >= from ? 1 : -1);
    this.step.set(next);
    this.motion.update((value) => value + 1);
  }

  backToIntent() {
    this.intent.set(null);
    this.step.set(Step.Specialty);
    this.furthest.set(Step.Specialty);
    this.offer.set(null);
    this.specialtyId.set('');
    this.studyId.set('');
    this.visitTypeIds.set([]);
    this.clearSchedule();
    this.error.set('');
  }

  pickIntent(kind: Intent) {
    this.intent.set(kind);
    this.step.set(Step.Specialty);
    this.furthest.set(Step.Specialty);
    this.offer.set(null);
    this.specialtyId.set('');
    this.studyId.set('');
    this.visitTypeIds.set([]);
    this.clearSchedule();
    this.error.set('');
  }

  pickSpecialty(option: { id: string; specialtyId: string }) {
    this.specialtyId.set(option.specialtyId);
    if (this.intent() === Intent.Estudio) {
      this.studyId.set(option.id);
      this.visitTypeIds.set([]);
    } else {
      this.studyId.set('');
      this.visitTypeIds.set(this.types().filter((type) => type.specialtyId === option.specialtyId && type.kind === AppointmentKind.Consulta).map((type) => type.id));
    }
    this.clearSchedule();
    this.furthest.set(Step.Specialty);
    this.error.set('');
    this.nearest.set(null);
    this.offer.set(Offer.Choose);
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
      this.offer.set(Offer.Confirm);
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
    this.go(Step.Day);
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
    await this.loadSlots(iso, true);
    if (!this.error()) this.go(Step.Time);
  }

  pickSlot(slot: SlotDto) {
    void this.applySlot(slot);
  }

  continueData() {
    const firstName = this.firstName.trim();
    const lastName = this.lastName.trim();
    const documentNumber = this.documentNumber.replace(/[.\-\s]/g, '');
    const phone = formatPhone(this.phone);
    if (!firstName || !lastName) {
      this.error.set('Escribí tu nombre y tu apellido.');
      return;
    }
    if (documentNumber.length < 6 || documentNumber.length > 20) {
      this.error.set('El documento no es válido.');
      return;
    }
    if (!phone) {
      this.error.set('El teléfono no es válido.');
      return;
    }
    this.firstName = firstName;
    this.lastName = lastName;
    this.documentNumber = documentNumber;
    this.phone = phone;
    this.error.set('');
    this.go(Step.Coverage);
  }

  pickCoverage(kind: Coverage) {
    this.error.set('');
    if (kind === Coverage.Particular || !this.insurers().length) {
      this.coverage.set(Coverage.Particular);
      this.insurerId = '';
      this.memberNumber = '';
      this.go(Step.Review);
      return;
    }
    this.coverage.set(Coverage.Obra);
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
    this.go(Step.Review);
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
        name: `${this.firstName.trim()} ${this.lastName.trim()}`,
        phone: this.phone.trim(),
        documentNumber: this.documentNumber.trim(),
        insurerId: this.coverage() === Coverage.Obra ? this.insurerId : null,
        memberNumber: this.coverage() === Coverage.Obra ? this.memberNumber.trim() : null,
      }));
      this.show(Step.Done);
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
      if (clinicaSession.role() === TenantRole.Patient) {
        const history = await firstValueFrom(this.clinical.clinicalHistory()).catch(() => null);
        if (history) {
          const parts = history.patient.name.trim().split(/\s+/);
          this.firstName = parts[0] ?? '';
          this.lastName = parts.slice(1).join(' ');
          this.phone = formatPhone(history.patient.phone) ?? history.patient.phone;
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
    const iso = slot.start.slice(0, 10);
    const [year, month] = iso.split('-').map(Number);
    this.dayIso.set(iso);
    this.year.set(year);
    this.month.set(month);
    this.slot.set(slot);
    void this.loadSlots(iso);
    this.coverage.set(Coverage.Particular);
    this.insurerId = '';
    this.memberNumber = '';
    this.error.set('');
    try {
      this.insurers.set(await firstValueFrom(this.organization.professionalInsurers(slot.professionalId)));
    } catch {
      this.insurers.set([]);
    }
    this.go(Step.Data);
  }

  private clearSchedule() {
    this.slot.set(null);
    this.dayIso.set('');
    this.slots.set([]);
    this.slotsFor.set('');
    this.nearest.set(null);
  }

  private async loadSlots(iso: string, spin = false) {
    if (spin) this.loading.set(true);
    this.error.set('');
    try {
      const lists = await Promise.all(this.locationIds().map((locationId) => firstValueFrom(this.schedule.availability({
        locationId,
        specialtyId: this.specialtyId(),
        date: iso,
      }))));
      const now = Date.now();
      this.dayIso.set(iso);
      this.slotsFor.set(iso);
      this.slots.set(lists.flat().filter((slot) => new Date(slot.start).getTime() > now && this.accepts(slot)).sort((left, right) => left.start.localeCompare(right.start)));
    } catch (error) {
      this.slots.set([]);
      this.slotsFor.set('');
      if (spin) this.error.set(errorMessage(error));
    } finally {
      if (spin) this.loading.set(false);
    }
  }

  private typeIds() {
    return this.intent() === Intent.Estudio ? [this.studyId()].filter((id) => id.length > 0) : this.visitTypeIds();
  }

  private accepts(slot: SlotDto) {
    if (this.intent() === Intent.Estudio) return slot.appointmentTypeId === this.studyId();
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
        appointmentTypeIds: this.typeIds(),
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
        appointmentTypeIds: this.typeIds(),
      }));
      this.openDays.set(days);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
