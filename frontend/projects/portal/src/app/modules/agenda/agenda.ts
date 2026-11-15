import { Component, DestroyRef, ElementRef, HostListener, computed, inject, signal, viewChild } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import {
  AgendaDayDto,
  AgendaProfessionalDto,
  AgendaService,
  AppointmentDto,
  AppointmentStatus,
  AppointmentsService,
  BookingRequestDto,
  TenantRole,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { RouteParam } from '../../app.routes.enum';
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
  shownStatus,
  spans,
  statusTone,
} from './agenda-time';
import { AgendaView, NO_ROOM, agendaViewLabels } from './agenda-view';
import { AgendaCreate } from './agenda-create';
import { AgendaDetail } from './agenda-detail';
import { AgendaRequest } from './agenda-request';

enum DropKind {
  Move = 'move',
  Request = 'request',
  Create = 'create',
}

// One column of the grid: a professional, or a room or piece of equipment (then professional is null).
type Column = { id: string; name: string; caption: string | null; professional: AgendaProfessionalDto | null };

// column is where the ghost is drawn; origin is the column the drag started from; professionalId is who attends.
type Drag = {
  kind: DropKind;
  appointment?: AppointmentDto;
  request?: BookingRequestDto;
  professionalId: string;
  column: string;
  origin: string;
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
  column: Column;
  start: number;
  duration: number;
};

type Draft = { professional: AgendaProfessionalDto; start: number };

const VIEW_KEY = 'agenda.view';

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
  readonly views = AgendaView;
  readonly viewOptions = (Object.keys(agendaViewLabels) as AgendaView[]).map((value) => ({ value, label: agendaViewLabels[value] }));
  // A professional only sees their own agenda, so the specialty and professional pickers are front desk's.
  readonly doctor = clinicaSession.role() === TenantRole.Doctor;
  readonly labels = appointmentStatusLabels;
  readonly tones = statusTone;
  readonly shown = shownStatus;
  // One entry per color, naming every status that shares it (e.g. "Reservado / Reprogramado").
  // A removed booking leaves the agenda, so its state has no place in the legend.
  readonly legend = (Object.keys(statusTone) as AppointmentStatus[]).filter((status) => status !== AppointmentStatus.Cancelled).reduce<{ tone: string; label: string }[]>((items, status) => {
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
  readonly view = signal(this.savedView());
  // An appointment asked for in the link, opened once its day has loaded.
  private pendingOpen: string | null = null;

  readonly title = computed(() => dayTitle(this.day()));
  readonly dayTitle = dayTitle;
  readonly isToday = computed(() => this.day() === localDate(new Date()));
  readonly canEdit = computed(() => this.data()?.canEdit ?? false);
  readonly byProfessional = computed(() => this.view() === AgendaView.Professional);
  readonly allProfessionals = computed(() => this.data()?.professionals ?? []);
  readonly maxOverbooks = computed(() => this.data()?.maxOverbooksPerDay ?? null);
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
  // The filters pick professionals; in the room and equipment views they pick whose appointments show.
  readonly appointments = computed(() => {
    const ids = new Set(this.professionals().map((item) => item.id));
    return (this.data()?.appointments ?? []).filter((item) => ids.has(item.professionalId));
  });
  readonly columns = computed((): Column[] => {
    const view = this.view();
    if (view === AgendaView.Professional)
      return this.professionals().map((item) => ({ id: item.id, name: item.name, caption: item.specialty ?? null, professional: item }));
    const resources = view === AgendaView.Room ? (this.data()?.rooms ?? []) : (this.data()?.equipment ?? []);
    const list: Column[] = resources.map((item) => ({ id: item.id, name: item.name, caption: item.location, professional: null }));
    if (view === AgendaView.Room && this.appointments().some((item) => !item.roomId))
      list.push({ id: NO_ROOM, name: 'Sin consultorio', caption: 'Turnos sin consultorio asignado', professional: null });
    return list;
  });
  readonly emptyMessage = computed(() => {
    if (this.loading()) return 'Cargando agenda…';
    if (this.view() === AgendaView.Room) return 'No hay consultorios cargados. Se cargan en la administración, en Consultorios y equipos.';
    if (this.view() === AgendaView.Equipment) return 'No hay equipos cargados. Se cargan en la administración, en Consultorios y equipos.';
    return this.filtering() && this.allProfessionals().length ? 'Ningún profesional coincide con los filtros este día' : 'Sin profesionales para mostrar';
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
    // The front desk opens an appointment here with ?fecha=…&turno=…: show its day and its detail.
    const params = inject(ActivatedRoute).snapshot.queryParamMap;
    const day = params.get(RouteParam.Fecha);
    if (day && /^\d{4}-\d{2}-\d{2}$/.test(day)) this.day.set(day);
    this.pendingOpen = params.get(RouteParam.Turno);
    void this.load();
    // Other people change the agenda too: refresh quietly while the tab is visible and nobody is mid-action here.
    const timer = setInterval(() => void this.refresh(), 30000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  private busy() {
    return this.loading() || this.saving() || this.picking() || !!this.drag() || !!this.drop() || !!this.draft() || !!this.open() || !!this.openRequest();
  }

  async refresh() {
    if (document.hidden || this.busy()) return;
    const day = this.day();
    try {
      const data = await firstValueFrom(this.agendaApi.day(day));
      const requests = data.canEdit ? await firstValueFrom(this.appointmentsApi.pendingRequests()) : [];
      if (day !== this.day() || this.busy()) return;
      this.data.set(data);
      this.remember(data.professionals);
      if (data.canEdit) this.requests.set(requests);
    } catch {
      // A failed background refresh keeps what is on screen; the next tick or any action tries again.
    }
  }

  readonly clock = clock;
  readonly minuteOf = minutesOf;
  readonly movable = movable;
  when = (iso: string) => formatFecha(iso);

  pickView(view: AgendaView) {
    this.view.set(view);
    try {
      localStorage.setItem(VIEW_KEY, view);
    } catch {
      // Without storage the view just starts by professional next time.
    }
  }

  private savedView() {
    try {
      const saved = localStorage.getItem(VIEW_KEY);
      return Object.values(AgendaView).includes(saved as AgendaView) ? (saved as AgendaView) : AgendaView.Professional;
    } catch {
      return AgendaView.Professional;
    }
  }

  // The column an appointment sits in for the current view; null when it has none there (no equipment).
  private columnOf(appointment: AppointmentDto): string | null {
    if (this.view() === AgendaView.Room) return appointment.roomId ?? NO_ROOM;
    if (this.view() === AgendaView.Equipment) return appointment.equipmentId ?? null;
    return appointment.professionalId;
  }

  column(col: Column) {
    const start = this.range().start;
    const shown = this.appointments().filter((item) => this.columnOf(item) === col.id);
    return {
      blockouts: spans(col.professional?.blockouts ?? []).map((range) => ({
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

  ghostIn(col: Column) {
    const drag = this.drag();
    if (drag?.moved && drag.column === col.id) return drag;
    const drop = this.drop();
    if (drop && drop.column.id === col.id) return { ...drop, professionalId: drop.professional.id, moved: true };
    return null;
  }

  hidden(appointment: AppointmentDto) {
    const drag = this.drag();
    const drop = this.drop();
    return (drag?.moved && drag.appointment?.id === appointment.id) || drop?.appointment?.id === appointment.id;
  }

  // Outside a professional's agenda the cell is shaded; rooms and equipment have no agenda of their own.
  inAgenda(col: Column, row: number) {
    return !col.professional || !isOverbook(col.professional, row, row + ROW_MINUTES);
  }

  ghostOverbook(professionalId: string, start: number, duration: number) {
    const professional = this.allProfessionals().find((item) => item.id === professionalId);
    return !!professional && isOverbook(professional, start, start + duration);
  }

  // Overbooks a professional already has this day, leaving out the one being moved.
  overbooksOf(professionalId: string, except?: string) {
    return (this.data()?.appointments ?? []).filter(
      (item) =>
        item.professionalId === professionalId &&
        item.isOverbook &&
        item.id !== except &&
        item.status !== AppointmentStatus.Cancelled &&
        item.status !== AppointmentStatus.NoShow,
    ).length;
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
    const column = this.columnOf(appointment) ?? '';
    const base = { kind: DropKind.Move, appointment, professionalId: appointment.professionalId, column, origin: column, start: from, duration: durationOf(appointment), anchor: from, x: event.clientX, y: event.clientY, moved: false };
    if (!this.canEdit() || !movable(appointment.status)) {
      this.drag.set({ ...base, grab: 0, locked: true });
      return;
    }
    event.preventDefault();
    const pointer = this.minuteAt(event.clientY, column) ?? from;
    this.drag.set({ ...base, grab: pointer - from });
  }

  // A request is placed on a professional, so it only drags in the view by professional.
  startRequest(event: PointerEvent, request: BookingRequestDto) {
    if (event.button !== 0 || !this.canEdit()) return;
    if (!this.byProfessional()) {
      this.openRequest.set(request);
      return;
    }
    event.preventDefault();
    const first = this.professionals().find((item) => item.specialty === request.specialty);
    const type = first?.types.find((item) => item.name === request.appointmentType);
    this.drag.set({
      kind: DropKind.Request,
      request,
      professionalId: first?.id ?? '',
      column: first?.id ?? '',
      origin: first?.id ?? '',
      start: this.range().start,
      duration: type?.durationMinutes ?? 30,
      anchor: this.range().start,
      grab: 0,
      x: event.clientX,
      y: event.clientY,
      moved: false,
    });
  }

  // A new appointment needs a professional, so it is drawn in the view by professional.
  startCreate(event: PointerEvent, col: Column) {
    if (event.button !== 0 || !this.canEdit() || this.drop() || !col.professional) return;
    const minute = this.minuteAt(event.clientY, col.id);
    if (minute === null) return;
    const start = this.snap(minute);
    this.drag.set({
      kind: DropKind.Create,
      professionalId: col.professional.id,
      column: col.id,
      origin: col.id,
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
      const minute = this.minuteAt(event.clientY, drag.column);
      if (minute === null) return;
      const edge = this.snap(minute);
      const start = Math.min(drag.anchor, edge);
      const end = Math.max(drag.anchor + SNAP_MINUTES, edge);
      this.drag.set({ ...drag, moved, start, duration: end - start });
      return;
    }
    const hovered = this.columns().find((item) => item.id === this.columnAt(event.clientX));
    const target = hovered && this.takes(hovered, drag) ? hovered : null;
    const column = target?.id ?? drag.column;
    const professionalId = target?.professional?.id ?? drag.professionalId;
    const minute = this.minuteAt(event.clientY, column);
    if (minute === null) return;
    const latest = this.range().end - drag.duration;
    const start = Math.max(this.range().start, Math.min(latest, this.snap(minute - drag.grab)));
    this.drag.set({ ...drag, moved, professionalId, column, start });
  }

  // While dragging, the columns that cannot take it fade out.
  accepts(col: Column) {
    const drag = this.drag();
    return !drag?.moved || this.takes(col, drag);
  }

  // By professional: only those who give that service (same specialty). By room: any room, the
  // professional stays. By equipment: the same equipment, since the practice decides which it needs.
  private takes(col: Column, drag: Drag) {
    if (col.professional) {
      if (drag.appointment) return col.professional.types.some((type) => type.id === drag.appointment!.appointmentTypeId);
      if (drag.request) return col.professional.specialty === drag.request.specialty;
      return true;
    }
    if (this.view() === AgendaView.Room) return col.id !== NO_ROOM || col.id === drag.origin;
    return col.id === drag.origin;
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
    const professional = this.allProfessionals().find((item) => item.id === drag.professionalId);
    const column = this.columns().find((item) => item.id === drag.column);
    if (!drag.moved) {
      if (drag.kind === DropKind.Move && drag.appointment) this.open.set(drag.appointment);
      if (drag.kind === DropKind.Create && professional && column)
        this.drop.set({ kind: DropKind.Create, professional, column, start: drag.start, duration: ROW_MINUTES });
      return;
    }
    if (!professional || !column) return;
    if (drag.kind === DropKind.Move && drag.appointment && drag.column === drag.origin && drag.professionalId === drag.appointment.professionalId && drag.start === minutesOf(drag.appointment.start)) return;
    this.drop.set({ kind: drag.kind, appointment: drag.appointment, request: drag.request, professional, column, start: drag.start, duration: drag.duration });
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

  // The clinic caps overbooks per professional and day; past the cap the drop cannot be confirmed.
  dropOverbookFull() {
    const drop = this.drop();
    const max = this.maxOverbooks();
    if (!drop || max === null || !this.dropOverbook()) return false;
    // Moving an overbook within the same day does not add one.
    const except = drop.appointment && drop.appointment.start.slice(0, 10) === this.day() ? drop.appointment.id : undefined;
    return this.overbooksOf(drop.professional.id, except) >= max;
  }

  // The room the drop puts the appointment in, when it was dropped on a room column.
  private dropRoom(drop: Drop) {
    return this.view() === AgendaView.Room && drop.column.id !== NO_ROOM ? drop.column.id : null;
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
        const roomId = this.dropRoom(drop);
        const moved = await this.call(this.agendaApi.move(drop.appointment.id, { professionalId: drop.professional.id, start, roomId }));
        const where = moved.room ? ` en ${moved.room}` : '';
        this.toasts.success('Turno movido', `${drop.appointment.patient} quedó a las ${clock(drop.start)} con ${drop.professional.name}${where}.`);
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
      if (this.pendingOpen) {
        const linked = this.data()?.appointments.find((item) => item.id === this.pendingOpen);
        this.pendingOpen = null;
        if (linked) this.open.set(linked);
      }
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

  private minuteAt(y: number, columnId: string) {
    const element = document.querySelector<HTMLElement>(`.ag-col[data-id="${columnId}"]`);
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
