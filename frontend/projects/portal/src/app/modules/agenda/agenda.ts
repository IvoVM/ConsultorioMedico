import { Component, ElementRef, HostListener, computed, inject, signal, viewChild } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import {
  AgendaDayDto,
  AgendaProfessionalDto,
  AgendaService,
  AppointmentDto,
  AppointmentStatus,
  AppointmentsService,
  BookingRequestDto,
  errorMessage,
} from 'sdk';
import { FormsModule } from '@angular/forms';
import { SelectOption, ToastService, UiAvatar, UiButton, UiIcon, UiModal, UiMonth, UiSelect, formatFecha, localDate } from 'ui';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import {
  PX_PER_MINUTE,
  ROW_MINUTES,
  SNAP_MINUTES,
  bounds,
  clock,
  dayTitle,
  durationOf,
  inPast,
  isOverbook,
  isoAt,
  minutesOf,
  movable,
  shiftDay,
  spans,
  statusTone,
} from './agenda-time';
import { AgendaCreate } from './agenda-create';
import { AgendaDetail } from './agenda-detail';
import { AgendaRequest } from './agenda-request';

enum DropKind {
  Move = 'move',
  Request = 'request',
  Create = 'create',
}

type Drag = {
  kind: DropKind;
  appointment?: AppointmentDto;
  request?: BookingRequestDto;
  professionalId: string;
  start: number;
  duration: number;
  anchor: number;
  grab: number;
  x: number;
  y: number;
  moved: boolean;
  locked?: boolean;
};

type Drop = {
  kind: DropKind;
  appointment?: AppointmentDto;
  request?: BookingRequestDto;
  professional: AgendaProfessionalDto;
  start: number;
  duration: number;
};

type Draft = { professional: AgendaProfessionalDto; start: number };

@Component({
  imports: [FormsModule, UiAvatar, UiButton, UiIcon, UiModal, UiMonth, UiSelect, AgendaCreate, AgendaDetail, AgendaRequest],
  templateUrl: './agenda.html',
})
export class AgendaPage {
  private readonly agendaApi = inject(AgendaService);
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly toasts = inject(ToastService);
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  readonly kinds = DropKind;
  readonly labels = appointmentStatusLabels;
  readonly tones = statusTone;
  // One entry per color, naming every status that shares it (e.g. "Ausente / Cancelado").
  readonly legend = (Object.keys(statusTone) as AppointmentStatus[]).reduce<{ tone: string; label: string }[]>((items, status) => {
    const same = items.find((item) => item.tone === statusTone[status]);
    if (same) same.label += ` / ${appointmentStatusLabels[status]}`;
    else items.push({ tone: statusTone[status], label: appointmentStatusLabels[status] });
    return items;
  }, []);
  readonly ppm = PX_PER_MINUTE;
  readonly rowHeight = ROW_MINUTES * PX_PER_MINUTE;
  readonly day = signal(localDate(new Date()));
  readonly data = signal<AgendaDayDto | null>(null);
  readonly requests = signal<BookingRequestDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly picking = signal(false);
  readonly pickYear = signal(new Date().getFullYear());
  readonly pickMonth = signal(new Date().getMonth() + 1);
  readonly drag = signal<Drag | null>(null);
  readonly drop = signal<Drop | null>(null);
  readonly saving = signal(false);
  readonly bounce = signal('');
  readonly draft = signal<Draft | null>(null);
  readonly open = signal<AppointmentDto | null>(null);
  readonly openRequest = signal<BookingRequestDto | null>(null);
  readonly helpOpen = signal(false);

  readonly title = computed(() => dayTitle(this.day()));
  readonly dayTitle = dayTitle;
  readonly isToday = computed(() => this.day() === localDate(new Date()));
  readonly canEdit = computed(() => this.data()?.canEdit ?? false);
  readonly allProfessionals = computed(() => this.data()?.professionals ?? []);
  readonly specialtyFilter = signal('');
  readonly professionalFilter = signal('');
  // Every professional seen while browsing days, so a chosen filter keeps its option on a day they don't show up.
  private readonly seen = signal(new Map<string, { name: string; specialty: string }>());
  readonly filtering = computed(() => !!this.specialtyFilter() || !!this.professionalFilter());
  readonly professionals = computed(() => {
    const specialty = this.specialtyFilter();
    const professional = this.professionalFilter();
    return this.allProfessionals().filter(
      (item) => (!specialty || item.specialty === specialty) && (!professional || item.id === professional),
    );
  });
  readonly specialtyOptions = computed((): SelectOption[] => {
    const names = new Set([...this.seen().values()].map((item) => item.specialty).filter(Boolean));
    return [{ value: '', label: 'Todas las especialidades' }, ...[...names].sort((a, b) => a.localeCompare(b, 'es')).map((name) => ({ value: name, label: name }))];
  });
  readonly professionalOptions = computed((): SelectOption[] => {
    const specialty = this.specialtyFilter();
    const options = [...this.seen()]
      .filter(([, item]) => !specialty || item.specialty === specialty)
      .map(([id, item]) => ({ value: id, label: item.name }))
      .sort((a, b) => a.label.localeCompare(b.label, 'es'));
    return [{ value: '', label: specialty ? `Todos los de ${specialty}` : 'Todos los profesionales' }, ...options];
  });
  readonly range = computed(() => bounds());
  readonly height = computed(() => (this.range().end - this.range().start) * PX_PER_MINUTE);
  readonly rows = computed(() => {
    const rows: number[] = [];
    for (let minute = this.range().start; minute < this.range().end; minute += ROW_MINUTES) rows.push(minute);
    return rows;
  });
  readonly monthDays = computed(() => {
    const count = new Date(this.pickYear(), this.pickMonth(), 0).getDate();
    return Array.from({ length: count }, (_, index) => `${this.pickYear()}-${String(this.pickMonth()).padStart(2, '0')}-${String(index + 1).padStart(2, '0')}`);
  });
  readonly nowLine = computed(() => {
    if (!this.isToday()) return null;
    const now = new Date();
    const minutes = now.getHours() * 60 + now.getMinutes();
    return minutes >= this.range().start && minutes <= this.range().end ? (minutes - this.range().start) * PX_PER_MINUTE : null;
  });

  constructor() {
    void this.load();
  }

  readonly clock = clock;
  readonly minuteOf = minutesOf;
  readonly movable = movable;
  when = (iso: string) => formatFecha(iso);

  column(professional: AgendaProfessionalDto) {
    const start = this.range().start;
    const shown = (this.data()?.appointments ?? []).filter((item) => item.professionalId === professional.id);
    return {
      blockouts: spans(professional.blockouts).map((range) => ({
        top: (Math.max(range.from, start) - start) * PX_PER_MINUTE,
        height: (Math.min(range.to, this.range().end) - Math.max(range.from, start)) * PX_PER_MINUTE,
      })),
      cards: shown.map((appointment) => {
        const from = minutesOf(appointment.start);
        const duration = durationOf(appointment);
        return { appointment, from, duration, top: (from - start) * PX_PER_MINUTE, height: Math.max(duration * PX_PER_MINUTE, 26) };
      }),
    };
  }

  top(minutes: number) {
    return (minutes - this.range().start) * PX_PER_MINUTE;
  }

  ghostIn(professional: AgendaProfessionalDto) {
    const drag = this.drag();
    if (drag?.moved && drag.professionalId === professional.id) return drag;
    const drop = this.drop();
    if (drop && drop.professional.id === professional.id) return { ...drop, professionalId: drop.professional.id, moved: true };
    return null;
  }

  hidden(appointment: AppointmentDto) {
    const drag = this.drag();
    const drop = this.drop();
    return (drag?.moved && drag.appointment?.id === appointment.id) || drop?.appointment?.id === appointment.id;
  }

  inAgenda(professional: AgendaProfessionalDto, row: number) {
    return !isOverbook(professional, row, row + ROW_MINUTES);
  }

  ghostOverbook(professional: AgendaProfessionalDto, start: number, duration: number) {
    return isOverbook(professional, start, start + duration);
  }

  async go(delta: number) {
    this.day.set(shiftDay(this.day(), delta));
    await this.load();
  }

  async today() {
    this.day.set(localDate(new Date()));
    await this.load();
  }

  togglePicker() {
    const [year, month] = this.day().split('-').map(Number);
    this.pickYear.set(year);
    this.pickMonth.set(month);
    this.picking.update((value) => !value);
  }

  shiftPicker(delta: number) {
    const next = new Date(this.pickYear(), this.pickMonth() - 1 + delta, 1);
    this.pickYear.set(next.getFullYear());
    this.pickMonth.set(next.getMonth() + 1);
  }

  async pickDay(iso: string) {
    this.picking.set(false);
    this.day.set(iso);
    await this.load();
  }

  startCard(event: PointerEvent, appointment: AppointmentDto) {
    if (event.button !== 0) return;
    event.stopPropagation();
    const from = minutesOf(appointment.start);
    if (!this.canEdit() || !movable(appointment.status)) {
      this.drag.set({ kind: DropKind.Move, appointment, professionalId: appointment.professionalId, start: from, duration: durationOf(appointment), anchor: from, grab: 0, x: event.clientX, y: event.clientY, moved: false, locked: true });
      return;
    }
    event.preventDefault();
    const pointer = this.minuteAt(event.clientY, appointment.professionalId) ?? from;
    this.drag.set({
      kind: DropKind.Move,
      appointment,
      professionalId: appointment.professionalId,
      start: from,
      duration: durationOf(appointment),
      anchor: from,
      grab: pointer - from,
      x: event.clientX,
      y: event.clientY,
      moved: false,
    });
  }

  startRequest(event: PointerEvent, request: BookingRequestDto) {
    if (event.button !== 0 || !this.canEdit()) return;
    event.preventDefault();
    const first = this.professionals().find((item) => item.specialty === request.specialty);
    const type = first?.types.find((item) => item.name === request.appointmentType);
    this.drag.set({
      kind: DropKind.Request,
      request,
      professionalId: first?.id ?? '',
      start: this.range().start,
      duration: type?.durationMinutes ?? 30,
      anchor: this.range().start,
      grab: 0,
      x: event.clientX,
      y: event.clientY,
      moved: false,
    });
  }

  startCreate(event: PointerEvent, professional: AgendaProfessionalDto) {
    if (event.button !== 0 || !this.canEdit() || this.drop()) return;
    const minute = this.minuteAt(event.clientY, professional.id);
    if (minute === null) return;
    const start = this.snap(minute);
    this.drag.set({
      kind: DropKind.Create,
      professionalId: professional.id,
      start,
      duration: ROW_MINUTES,
      anchor: start,
      grab: 0,
      x: event.clientX,
      y: event.clientY,
      moved: false,
    });
  }

  @HostListener('document:pointermove', ['$event'])
  onMove(event: PointerEvent) {
    const drag = this.drag();
    if (!drag || drag.locked) return;
    const moved = drag.moved || Math.hypot(event.clientX - drag.x, event.clientY - drag.y) > 5;
    if (!moved) return;
    this.autoScroll(event);
    if (drag.kind === DropKind.Create) {
      const minute = this.minuteAt(event.clientY, drag.professionalId);
      if (minute === null) return;
      const edge = this.snap(minute);
      const start = Math.min(drag.anchor, edge);
      const end = Math.max(drag.anchor + SNAP_MINUTES, edge);
      this.drag.set({ ...drag, moved, start, duration: end - start });
      return;
    }
    const hovered = this.allProfessionals().find((item) => item.id === this.columnAt(event.clientX));
    const professional = hovered && this.serves(hovered, drag) ? hovered.id : drag.professionalId;
    const minute = this.minuteAt(event.clientY, professional);
    if (minute === null) return;
    const latest = this.range().end - drag.duration;
    const start = Math.max(this.range().start, Math.min(latest, this.snap(minute - drag.grab)));
    this.drag.set({ ...drag, moved, professionalId: professional, start });
  }

  // While dragging an appointment or a request, only professionals who give that service (same specialty) can take it.
  accepts(professional: AgendaProfessionalDto) {
    const drag = this.drag();
    return !drag?.moved || this.serves(professional, drag);
  }

  private serves(professional: AgendaProfessionalDto, drag: Drag) {
    if (drag.appointment) return professional.types.some((type) => type.id === drag.appointment!.appointmentTypeId);
    if (drag.request) return professional.specialty === drag.request.specialty;
    return true;
  }

  @HostListener('document:pointerup')
  onUp() {
    const drag = this.drag();
    if (!drag) return;
    this.drag.set(null);
    if (!drag.moved && drag.kind === DropKind.Request && drag.request) {
      this.openRequest.set(drag.request);
      return;
    }
    const professional = this.professionals().find((item) => item.id === drag.professionalId);
    if (!professional) return;
    if (!drag.moved) {
      if (drag.kind === DropKind.Move && drag.appointment) this.open.set(drag.appointment);
      if (drag.kind === DropKind.Create) this.drop.set({ kind: DropKind.Create, professional, start: drag.start, duration: ROW_MINUTES });
      return;
    }
    if (drag.kind === DropKind.Move && drag.appointment && drag.professionalId === drag.appointment.professionalId && drag.start === minutesOf(drag.appointment.start)) return;
    this.drop.set({ kind: drag.kind, appointment: drag.appointment, request: drag.request, professional, start: drag.start, duration: drag.duration });
  }

  @HostListener('document:pointercancel')
  onCancel() {
    this.drag.set(null);
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.drag()) this.drag.set(null);
  }

  cancelDrop() {
    const drop = this.drop();
    this.drop.set(null);
    if (drop?.appointment) this.flash(drop.appointment.id);
  }

  dropPast() {
    const drop = this.drop();
    return !!drop && inPast(this.day(), drop.start);
  }

  dropOverbook() {
    const drop = this.drop();
    return !!drop && isOverbook(drop.professional, drop.start, drop.start + drop.duration);
  }

  async confirmDrop() {
    const drop = this.drop();
    if (!drop) return;
    if (drop.kind === DropKind.Create) {
      this.drop.set(null);
      this.draft.set({ professional: drop.professional, start: drop.start });
      return;
    }
    this.saving.set(true);
    const start = isoAt(this.day(), drop.start);
    try {
      if (drop.kind === DropKind.Move && drop.appointment) {
        await this.call(this.agendaApi.move(drop.appointment.id, { professionalId: drop.professional.id, start }));
        this.toasts.success('Turno movido', `${drop.appointment.patient} quedó a las ${clock(drop.start)} con ${drop.professional.name}.`);
      } else if (drop.kind === DropKind.Request && drop.request) {
        await this.call(this.agendaApi.placeRequest(drop.request.id, { professionalId: drop.professional.id, start }));
        this.toasts.success('Turno dado', `${drop.request.name} quedó a las ${clock(drop.start)} con ${drop.professional.name}.`);
      }
      this.drop.set(null);
      await this.load();
    } catch (error) {
      this.toasts.error('No se pudo hacer el cambio', error instanceof Error ? error.message : 'Volvió a como estaba.');
      this.drop.set(null);
      if (drop.appointment) this.flash(drop.appointment.id);
    } finally {
      this.saving.set(false);
    }
  }

  async afterChange() {
    this.open.set(null);
    this.openRequest.set(null);
    this.draft.set(null);
    await this.load();
  }

  pickSpecialty(specialty: string) {
    this.specialtyFilter.set(specialty);
    const chosen = this.seen().get(this.professionalFilter());
    if (specialty && chosen && chosen.specialty !== specialty) this.professionalFilter.set('');
  }

  pickProfessional(id: string) {
    this.professionalFilter.set(id);
    const chosen = this.seen().get(id);
    if (chosen?.specialty) this.specialtyFilter.set(chosen.specialty);
  }

  clearFilters() {
    this.specialtyFilter.set('');
    this.professionalFilter.set('');
  }

  async load() {
    this.loading.set(true);
    this.error.set('');
    try {
      this.data.set(await firstValueFrom(this.agendaApi.day(this.day())));
      this.remember(this.data()?.professionals ?? []);
      if (this.data()?.canEdit) this.requests.set(await firstValueFrom(this.appointmentsApi.pendingRequests()));
    } catch (error) {
      this.data.set(null);
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
      setTimeout(() => this.focusTime());
    }
  }

  private remember(professionals: AgendaProfessionalDto[]) {
    if (professionals.every((item) => this.seen().has(item.id))) return;
    this.seen.update((current) => {
      const next = new Map(current);
      for (const item of professionals) next.set(item.id, { name: item.name, specialty: item.specialty ?? '' });
      return next;
    });
  }

  private focusTime() {
    const element = this.scroller()?.nativeElement;
    if (!element) return;
    const data = this.data() ?? { appointments: [], professionals: [] };
    const now = new Date();
    const firsts = [
      ...data.appointments.map((item) => minutesOf(item.start)),
      ...data.professionals.flatMap((item) => spans(item.ranges).map((range) => range.from)),
    ];
    const target = this.isToday() ? now.getHours() * 60 + now.getMinutes() - 60 : firsts.length ? Math.min(...firsts) - 30 : 7 * 60 + 30;
    element.scrollTo({ top: Math.max(0, this.top(target)), behavior: 'smooth' });
  }

  private flash(id: string) {
    this.bounce.set(id);
    setTimeout(() => this.bounce.set(''), 700);
  }

  private snap(minute: number) {
    return Math.round(minute / SNAP_MINUTES) * SNAP_MINUTES;
  }

  private columnAt(x: number) {
    const columns = Array.from(document.querySelectorAll<HTMLElement>('.ag-col'));
    const hit = columns.find((element) => {
      const rect = element.getBoundingClientRect();
      return x >= rect.left && x <= rect.right;
    });
    return hit?.dataset['id'] ?? null;
  }

  private minuteAt(y: number, professionalId: string) {
    const element = document.querySelector<HTMLElement>(`.ag-col[data-id="${professionalId}"]`);
    if (!element) return null;
    const rect = element.getBoundingClientRect();
    const minute = this.range().start + (y - rect.top) / PX_PER_MINUTE;
    return Math.max(this.range().start, Math.min(this.range().end - SNAP_MINUTES, minute));
  }

  private autoScroll(event: PointerEvent) {
    const element = this.scroller()?.nativeElement;
    if (!element) return;
    const rect = element.getBoundingClientRect();
    const edge = 48;
    if (event.clientY < rect.top + edge) element.scrollBy({ top: -14 });
    else if (event.clientY > rect.bottom - edge) element.scrollBy({ top: 14 });
    if (event.clientX < rect.left + edge) element.scrollBy({ left: -14 });
    else if (event.clientX > rect.right - edge) element.scrollBy({ left: 14 });
  }

  private async call<T>(request: Observable<T>) {
    try {
      return await firstValueFrom(request);
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  }
}
