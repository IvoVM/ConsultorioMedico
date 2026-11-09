import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { AgendaDayDto, AgendaProfessionalDto, AgendaService, AppointmentDto, AppointmentStatus, AppointmentsService, TenantRole, clinicaSession, errorMessage } from 'sdk';
import { SelectOption, ToastService, UiAvatar, UiButton, UiField, UiIcon, UiModal, UiMonth, UiSelect, formatFecha, localDate } from 'ui';
import { AppRoute } from '../../app.routes.enum';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { bounds, clock, dayTitle, durationOf, inPast, isOverbook, isoAt, minutesOf, movable, spans, statusTone } from './agenda-time';

enum DetailMode {
  Info = 'info',
  Edit = 'edit',
  Move = 'move',
  Confirm = 'confirm',
}

type Confirmation = {
  title: string;
  message: string;
  danger?: boolean;
  back: DetailMode;
  run: () => Promise<void>;
};

type TimeOption = { start: number; free: boolean; overbook: boolean };

const STEP = 15;

@Component({
  selector: 'agenda-detail',
  imports: [FormsModule, RouterLink, UiAvatar, UiButton, UiField, UiIcon, UiModal, UiMonth, UiSelect],
  templateUrl: './agenda-detail.html',
})
export class AgendaDetail {
  private readonly agendaApi = inject(AgendaService);
  private readonly appointmentsApi = inject(AppointmentsService);
  private readonly toasts = inject(ToastService);

  readonly appointment = input.required<AppointmentDto>();
  readonly professionals = input<AgendaProfessionalDto[]>([]);
  readonly canEdit = input(false);
  readonly changed = output<void>();
  readonly closed = output<void>();

  readonly modes = DetailMode;
  readonly route = AppRoute;
  readonly canSeeHistory = computed(() => clinicaSession.role() === TenantRole.Doctor);
  readonly status = AppointmentStatus;
  readonly labels = appointmentStatusLabels;
  readonly tones = statusTone;
  readonly mode = signal(DetailMode.Info);
  readonly confirmation = signal<Confirmation | null>(null);
  readonly saving = signal(false);
  readonly error = signal('');

  reason = '';
  insurerId = '';
  memberNumber = '';

  readonly moveYear = signal(new Date().getFullYear());
  readonly moveMonth = signal(new Date().getMonth() + 1);
  readonly moveDay = signal('');
  readonly moveData = signal<AgendaDayDto | null>(null);
  readonly moveLoading = signal(false);
  readonly moveProfessional = signal('');
  readonly moveStart = signal<number | null>(null);

  readonly from = computed(() => minutesOf(this.appointment().start));
  readonly duration = computed(() => durationOf(this.appointment()));
  readonly day = computed(() => this.appointment().start.slice(0, 10));
  readonly professional = computed(() => this.professionals().find((item) => item.id === this.appointment().professionalId) ?? null);
  readonly insurerOptions = computed((): SelectOption[] => [
    { value: '', label: 'Particular' },
    ...(this.professional()?.insurers ?? []).map((insurer) => ({ value: insurer.id, label: insurer.name })),
  ]);
  readonly canMove = computed(() => this.canEdit() && movable(this.appointment().status));
  readonly canCheckIn = computed(() => this.canEdit() && (this.appointment().status === AppointmentStatus.Booked || this.appointment().status === AppointmentStatus.Rescheduled));
  readonly canEditData = computed(() => this.canEdit() && this.appointment().status !== AppointmentStatus.Completed);
  readonly openDays = computed(() => {
    const today = localDate(new Date());
    const count = new Date(this.moveYear(), this.moveMonth(), 0).getDate();
    return Array.from({ length: count }, (_, index) => `${this.moveYear()}-${String(this.moveMonth()).padStart(2, '0')}-${String(index + 1).padStart(2, '0')}`).filter((iso) => iso >= today);
  });
  readonly moveCandidates = computed(() => (this.moveData()?.professionals ?? []).filter((item) => item.types.some((type) => type.id === this.appointment().appointmentTypeId)));
  readonly moveOptions = computed((): SelectOption[] => this.moveCandidates().map((item) => ({ value: item.id, label: item.name })));
  readonly moveTarget = computed(() => this.moveCandidates().find((item) => item.id === this.moveProfessional()) ?? null);
  readonly times = computed((): TimeOption[] => {
    const data = this.moveData();
    const target = this.moveTarget();
    if (!data || !target) return [];
    const day = this.moveDay();
    const own = this.appointment();
    const busy = data.appointments
      .filter((item) => item.professionalId === target.id && item.id !== own.id && item.status !== AppointmentStatus.Cancelled && item.status !== AppointmentStatus.NoShow)
      .map((item) => ({ from: minutesOf(item.start), to: minutesOf(item.start) + durationOf(item) }));
    const offs = spans(target.blockouts);
    const range = bounds();
    const length = this.duration();
    const options: TimeOption[] = [];
    for (let start = range.start; start + length <= range.end; start += STEP) {
      const end = start + length;
      const clash = busy.some((item) => item.from < end && start < item.to) || offs.some((item) => item.from < end && start < item.to);
      options.push({ start, free: !clash && !inPast(day, start), overbook: isOverbook(target, start, end) });
    }
    return options.filter((option) => option.free);
  });

  readonly clock = clock;
  readonly dayTitle = dayTitle;
  readonly when = (iso: string) => formatFecha(iso);

  edit() {
    const appointment = this.appointment();
    this.reason = appointment.visitReason ?? '';
    const insurer = this.professional()?.insurers.find((item) => item.name === appointment.insurer);
    this.insurerId = insurer?.id ?? '';
    this.memberNumber = appointment.memberNumber ?? '';
    this.error.set('');
    this.mode.set(DetailMode.Edit);
  }

  reviewEdit() {
    this.ask({
      title: 'Guardar cambios',
      message: `Se actualizan el motivo y la cobertura del turno de ${this.appointment().patient}.`,
      back: DetailMode.Edit,
      run: async () => {
        await this.call(this.agendaApi.edit(this.appointment().id, {
          visitReason: this.reason.trim() || null,
          insurerId: this.insurerId || null,
          memberNumber: this.insurerId ? this.memberNumber.trim() : null,
        }));
        this.toasts.success('Turno actualizado', `Se guardaron los cambios de ${this.appointment().patient}.`);
      },
    });
  }

  async startMove() {
    const today = localDate(new Date());
    const day = this.day() >= today ? this.day() : today;
    const [year, month] = day.split('-').map(Number);
    this.moveYear.set(year);
    this.moveMonth.set(month);
    this.moveProfessional.set(this.appointment().professionalId);
    this.error.set('');
    this.mode.set(DetailMode.Move);
    await this.pickMoveDay(day);
  }

  shiftMoveMonth(delta: number) {
    const next = new Date(this.moveYear(), this.moveMonth() - 1 + delta, 1);
    this.moveYear.set(next.getFullYear());
    this.moveMonth.set(next.getMonth() + 1);
  }

  async pickMoveDay(iso: string) {
    this.moveDay.set(iso);
    this.moveStart.set(null);
    this.moveLoading.set(true);
    try {
      this.moveData.set(await firstValueFrom(this.agendaApi.day(iso)));
      if (!this.moveCandidates().some((item) => item.id === this.moveProfessional())) this.moveProfessional.set(this.moveCandidates()[0]?.id ?? '');
    } catch (error) {
      this.moveData.set(null);
      this.error.set(errorMessage(error));
    } finally {
      this.moveLoading.set(false);
    }
  }

  pickProfessional(id: string) {
    this.moveProfessional.set(id);
    this.moveStart.set(null);
  }

  reviewMove() {
    const start = this.moveStart();
    const target = this.moveTarget();
    if (start === null || !target) return;
    const overbook = isOverbook(target, start, start + this.duration());
    this.ask({
      title: 'Reprogramar turno',
      message: `${this.appointment().patient} pasa al ${dayTitle(this.moveDay())} a las ${clock(start)} con ${target.name}.${overbook ? ' Queda como sobreturno porque está fuera de su agenda.' : ''}`,
      back: DetailMode.Move,
      run: async () => {
        await this.call(this.agendaApi.move(this.appointment().id, { professionalId: target.id, start: isoAt(this.moveDay(), start) }));
        this.toasts.success('Turno reprogramado', `${this.appointment().patient} quedó el ${dayTitle(this.moveDay())} a las ${clock(start)}.`);
      },
    });
  }

  checkIn() {
    this.ask({
      title: 'Pasar a sala de espera',
      message: `${this.appointment().patient} queda en sala de espera.`,
      back: DetailMode.Info,
      run: async () => {
        await this.call(this.appointmentsApi.checkInAppointment(this.appointment().id));
        this.toasts.success('En sala de espera', `${this.appointment().patient} está en sala de espera.`);
      },
    });
  }

  noShow() {
    this.ask({
      title: 'Marcar ausente',
      message: `El turno de ${this.appointment().patient} queda como ausente.`,
      back: DetailMode.Info,
      run: async () => {
        await this.call(this.appointmentsApi.markNoShow(this.appointment().id));
        this.toasts.success('Marcado ausente', `${this.appointment().patient} no vino.`);
      },
    });
  }

  cancel() {
    this.ask({
      title: 'Cancelar turno',
      message: `Se cancela el turno de ${this.appointment().patient} y el horario queda libre. No se puede deshacer.`,
      danger: true,
      back: DetailMode.Info,
      run: async () => {
        await this.call(this.appointmentsApi.cancelAppointment(this.appointment().id));
        this.toasts.success('Turno cancelado', `El horario de las ${clock(this.from())} quedó libre.`);
      },
    });
  }

  back() {
    const confirmation = this.confirmation();
    this.mode.set(confirmation?.back ?? DetailMode.Info);
    this.confirmation.set(null);
    this.error.set('');
  }

  async confirm() {
    const confirmation = this.confirmation();
    if (!confirmation) return;
    this.saving.set(true);
    this.error.set('');
    try {
      await confirmation.run();
      this.changed.emit();
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'No se pudo completar.');
    } finally {
      this.saving.set(false);
    }
  }

  private ask(confirmation: Confirmation) {
    this.confirmation.set(confirmation);
    this.error.set('');
    this.mode.set(DetailMode.Confirm);
  }

  private async call<T>(request: Observable<T>) {
    try {
      return await firstValueFrom(request);
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  }
}
