import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AgendaProfessionalDto, AgendaService, PatientLookupDto, PatientsService, errorMessage } from 'sdk';
import { SelectOption, ToastService, UiButton, UiField, UiIcon, UiModal, UiSelect, formatPhone } from 'ui';
import { clock, dayTitle, inPast, isOverbook, isoAt } from './agenda-time';

enum CreateStep {
  Form = 'form',
  Review = 'review',
}

@Component({
  selector: 'agenda-create',
  imports: [FormsModule, UiButton, UiField, UiIcon, UiModal, UiSelect],
  templateUrl: './agenda-create.html',
})
export class AgendaCreate {
  private readonly agendaApi = inject(AgendaService);
  private readonly patientsApi = inject(PatientsService);
  private readonly toasts = inject(ToastService);

  readonly professional = input.required<AgendaProfessionalDto>();
  readonly day = input.required<string>();
  readonly start = input.required<number>();
  readonly created = output<void>();
  readonly closed = output<void>();

  readonly steps = CreateStep;
  readonly step = signal(CreateStep.Form);
  readonly patient = signal<PatientLookupDto | null>(null);
  readonly unknown = signal(false);
  readonly looking = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly typeId = signal('');

  documentNumber = '';
  firstName = '';
  lastName = '';
  phone = '';
  reason = '';
  insurerId = '';
  memberNumber = '';

  readonly typeOptions = computed((): SelectOption[] => this.professional().types.map((type) => ({ value: type.id, label: `${type.name} · ${type.durationMinutes} min` })));
  readonly insurerOptions = computed((): SelectOption[] => [
    { value: '', label: 'Particular' },
    ...this.professional().insurers.map((insurer) => ({ value: insurer.id, label: insurer.name })),
  ]);
  readonly type = computed(() => this.professional().types.find((type) => type.id === this.typeId()) ?? null);
  readonly end = computed(() => this.start() + (this.type()?.durationMinutes ?? 0));
  readonly overbook = computed(() => isOverbook(this.professional(), this.start(), this.end()));
  readonly past = computed(() => inPast(this.day(), this.start()));
  readonly patientName = computed(() => {
    const found = this.patient();
    return found ? `${found.firstName} ${found.lastName}` : `${this.firstName.trim()} ${this.lastName.trim()}`.trim();
  });

  readonly clock = clock;
  readonly dayTitle = dayTitle;

  constructor() {
    queueMicrotask(() => {
      const types = this.professional().types;
      if (types.length === 1) this.typeId.set(types[0].id);
    });
  }

  async lookup() {
    const document = this.documentNumber.trim();
    this.patient.set(null);
    this.unknown.set(false);
    if (!document) return;
    this.looking.set(true);
    this.error.set('');
    try {
      this.patient.set(await firstValueFrom(this.patientsApi.patientByDocument(document)));
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) this.unknown.set(true);
      else this.error.set(errorMessage(error));
    } finally {
      this.looking.set(false);
    }
  }

  async review() {
    this.error.set('');
    if (!this.patient() && !this.unknown()) await this.lookup();
    if (!this.patient() && !this.unknown()) {
      this.error.set('Buscá al paciente por documento.');
      return;
    }
    if (this.unknown() && (!this.firstName.trim() || !this.lastName.trim() || !this.phone.trim())) {
      this.error.set('No hay un paciente con ese documento: cargá nombre, apellido y teléfono.');
      return;
    }
    if (!this.type()) {
      this.error.set('Elegí la prestación.');
      return;
    }
    if (this.insurerId && !this.memberNumber.trim()) {
      this.error.set('Con obra social, el número de afiliado es obligatorio.');
      return;
    }
    if (this.unknown()) this.phone = formatPhone(this.phone) ?? this.phone;
    this.step.set(CreateStep.Review);
  }

  async create() {
    const type = this.type();
    if (!type) return;
    this.saving.set(true);
    this.error.set('');
    try {
      await firstValueFrom(this.agendaApi.book({
        documentNumber: this.documentNumber.trim(),
        firstName: this.unknown() ? this.firstName.trim() : null,
        lastName: this.unknown() ? this.lastName.trim() : null,
        phone: this.unknown() ? this.phone : null,
        professionalId: this.professional().id,
        appointmentTypeId: type.id,
        start: isoAt(this.day(), this.start()),
        visitReason: this.reason.trim() || null,
        insurerId: this.insurerId || null,
        memberNumber: this.insurerId ? this.memberNumber.trim() : null,
      }));
      this.toasts.success(this.overbook() ? 'Sobreturno creado' : 'Turno creado', `${this.patientName()} quedó a las ${clock(this.start())} con ${this.professional().name}.`);
      this.created.emit();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
