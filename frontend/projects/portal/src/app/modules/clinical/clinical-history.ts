import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  BackgroundKind,
  ClinicalAccessDto,
  ClinicalEntryDto,
  ClinicalEntryKind,
  ClinicalHistoryDto,
  ClinicalService,
  ConsentDecision,
  TenantRole,
  clinicaSession,
  errorMessage,
} from 'sdk';
import { RouteParam } from '../../app.routes.enum';
import { FechaPipe, TabItem, UiBadge, UiButton, UiEmpty, UiField, UiSkeleton, UiTabs } from 'ui';
import { firstValueFrom } from 'rxjs';
import {
  accessLabels,
  backgroundKinds,
  backgroundLabels,
  bloodTypes,
  consentFileTypes,
  consentLabels,
  consentMaxBytes,
  downloadFile,
  entryTitle,
  readBase64,
  roleLabels,
  vitals,
} from './models/clinical-labels';
import { SignModal } from './sign-modal';
import { Stamp } from './stamp';

// What the professional is about to sign from this screen.
enum Signing {
  Background = 'background',
  Consent = 'consent',
  Amendment = 'amendment',
}

// The patient's single clinical record: background every professional sees, then the signed folios of the
// specialty, newest first. The professional adds background, consents and corrections, each signed; the patient
// reads all of it, downloads the copy the law entitles them to and sees who opened it.
@Component({
  imports: [FormsModule, FechaPipe, UiBadge, UiButton, UiEmpty, UiField, UiSkeleton, UiTabs, SignModal, Stamp],
  templateUrl: './clinical-history.html',
})
export class ClinicalHistoryPage {
  private readonly clinical = inject(ClinicalService);
  private readonly route = inject(ActivatedRoute);
  readonly session = clinicaSession;
  readonly roles = TenantRole;
  readonly kinds = ClinicalEntryKind;
  readonly signing = Signing;
  readonly backgroundKindEnum = BackgroundKind;
  readonly backgroundLabels = backgroundLabels;
  readonly backgroundKinds = backgroundKinds;
  readonly bloodTypes = bloodTypes;
  readonly decisions = Object.values(ConsentDecision);
  readonly consentLabels = consentLabels;
  readonly accessLabels = accessLabels;
  readonly roleLabels = roleLabels;
  readonly entryTitle = entryTitle;
  readonly vitals = vitals;
  readonly history = signal<ClinicalHistoryDto | null>(null);
  readonly accesses = signal<ClinicalAccessDto[] | null>(null);
  readonly error = signal('');
  readonly notice = signal('');
  readonly loading = signal(false);
  readonly downloading = signal(false);
  readonly form = signal<Signing | null>(null);
  readonly amending = signal<ClinicalEntryDto | null>(null);
  readonly busy = signal(false);
  readonly signError = signal('');
  // The handwritten signature of each signed page, by page. A page never changes, so neither does its picture.
  readonly signatures = signal<Record<string, string>>({});
  readonly tabs = computed((): TabItem[] =>
    (this.history()?.specialties ?? []).map((item) => ({ key: item.id, label: item.name, count: item.encounters })),
  );
  readonly allergies = computed(() => this.history()?.background.find((item) => item.kind === BackgroundKind.Allergies) ?? null);
  readonly isDoctor = computed(() => clinicaSession.role() === TenantRole.Doctor);
  patientId = '';
  backgroundKind = BackgroundKind.Allergies;
  backgroundText = '';
  procedure = '';
  decision = ConsentDecision.Accepted;
  consentFile: File | null = null;
  amendText = '';

  constructor() {
    effect(() => {
      const history = this.history();
      untracked(() => void this.loadSignatures(history));
    });
    inject(DestroyRef).onDestroy(() => Object.values(this.signatures()).forEach((url) => URL.revokeObjectURL(url)));
    const paciente = this.route.snapshot.queryParamMap.get(RouteParam.Paciente);
    if (paciente) this.patientId = paciente;
    if (clinicaSession.role() === TenantRole.Patient || paciente) void this.load();
  }

  async load(specialtyId?: string) {
    this.error.set('');
    this.loading.set(true);
    try {
      const patient = this.isDoctor() ? { patientId: this.patientId } : {};
      const query = specialtyId ? { ...patient, specialtyId } : patient;
      this.history.set(await firstValueFrom(this.clinical.clinicalHistory(query)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  pick(specialtyId: string) {
    if (specialtyId !== this.history()?.specialty?.id) void this.load(specialtyId);
  }

  private async loadSignatures(history: ClinicalHistoryDto | null) {
    const missing = (history?.entries ?? []).filter((entry) => entry.hasSignatureImage && !this.signatures()[entry.id]);
    await Promise.all(
      missing.map(async (entry) => {
        try {
          const blob = await firstValueFrom(this.clinical.entrySignature(entry.id));
          this.signatures.update((all) => ({ ...all, [entry.id]: URL.createObjectURL(blob) }));
        } catch {
          // Without the picture the stamp still shows who signed.
        }
      }),
    );
  }

  amendmentsOf(entry: ClinicalEntryDto) {
    return (this.history()?.entries ?? []).filter((item) => item.amendsId === entry.id);
  }

  async copy() {
    const history = this.history();
    if (!history) return;
    this.downloading.set(true);
    this.error.set('');
    try {
      const query = this.isDoctor() ? { patientId: this.patientId } : {};
      await downloadFile(this.clinical.clinicalHistoryCopy(query), `historia-clinica-${history.patient.historyNumber}.pdf`);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.downloading.set(false);
    }
  }

  async attachment(entry: ClinicalEntryDto) {
    this.error.set('');
    try {
      await downloadFile(this.clinical.entryAttachment(entry.id), `consentimiento-folio-${entry.folio}`);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async showAccesses() {
    try {
      this.accesses.set(await firstValueFrom(this.clinical.myClinicalAccesses()));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  start(form: Signing, entry?: ClinicalEntryDto) {
    this.signError.set('');
    this.notice.set('');
    this.form.set(form);
    this.amending.set(entry ?? null);
    this.backgroundText = '';
    this.procedure = '';
    this.decision = ConsentDecision.Accepted;
    this.consentFile = null;
    this.amendText = '';
  }

  pickFile(event: Event) {
    this.consentFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  async sign(password: string) {
    const form = this.form();
    const patientId = this.history()?.patient.id;
    if (!form || !patientId) return;
    this.busy.set(true);
    this.signError.set('');
    try {
      const entry = await this.write(form, patientId, password);
      this.form.set(null);
      this.notice.set(`Quedó firmado como folio ${entry.folio}.`);
      await this.load(this.history()?.specialty?.id);
    } catch (error) {
      this.signError.set(errorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }

  private async write(form: Signing, patientId: string, password: string) {
    switch (form) {
      case Signing.Background:
        return firstValueFrom(this.clinical.addBackground({ patientId, kind: this.backgroundKind, text: this.backgroundText, password }));
      case Signing.Amendment:
        return firstValueFrom(this.clinical.amendEntry(this.amending()!.id, { text: this.amendText, password }));
      case Signing.Consent: {
        const file = this.consentFile;
        if (!file) throw new Error('Adjuntá el consentimiento firmado por el paciente.');
        if (!consentFileTypes.includes(file.type)) throw new Error('El consentimiento tiene que ser PDF, JPG o PNG.');
        if (file.size > consentMaxBytes) throw new Error('El consentimiento tiene que pesar hasta 5 MB.');
        return firstValueFrom(
          this.clinical.addConsent({
            patientId,
            procedure: this.procedure,
            decision: this.decision,
            fileName: file.name,
            contentType: file.type,
            contentBase64: await readBase64(file),
            password,
          }),
        );
      }
    }
  }

  title(form: Signing) {
    switch (form) {
      case Signing.Background:
        return 'Agregar antecedente';
      case Signing.Consent:
        return 'Registrar consentimiento informado';
      case Signing.Amendment:
        return `Corregir el folio ${this.amending()?.folio ?? ''}`;
    }
  }
}
