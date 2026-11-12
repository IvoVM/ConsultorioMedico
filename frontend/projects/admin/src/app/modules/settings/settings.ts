import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicService, ClinicSettingsDto, errorMessage } from 'sdk';
import { TabItem, ToastService, UiButton, UiField, UiSkeleton, UiSwitch, UiTabs } from 'ui';
import { firstValueFrom } from 'rxjs';
import { clinicName } from '../../clinic-name';
import { SettingsTab, settingsTabLabels } from './models/settings-tab';

@Component({
  imports: [FormsModule, UiButton, UiField, UiSkeleton, UiSwitch, UiTabs],
  templateUrl: './settings.html',
})
export class SettingsPage {
  private readonly clinic = inject(ClinicService);
  private readonly toasts = inject(ToastService);
  readonly settingsTab = SettingsTab;
  readonly tabs: TabItem[] = [
    { key: SettingsTab.General, label: settingsTabLabels[SettingsTab.General], icon: 'apartment' },
    { key: SettingsTab.Facturacion, label: settingsTabLabels[SettingsTab.Facturacion], icon: 'receipt_long' },
  ];
  readonly tab = signal<string>(SettingsTab.General);
  readonly saved = signal<ClinicSettingsDto | null>(null);
  readonly draft = signal<ClinicSettingsDto | null>(null);
  readonly error = signal('');
  readonly saving = signal(false);
  // Nothing reaches the server until the admin saves, so the screen compares the draft with what is stored.
  readonly dirty = computed(() => {
    const saved = this.saved();
    const draft = this.draft();
    return !!saved && !!draft && (draft.name !== saved.name || draft.billingEnabled !== saved.billingEnabled || draft.autoInvoice !== saved.autoInvoice);
  });

  constructor() {
    void firstValueFrom(this.clinic.clinicSettings())
      .then((settings) => this.reset(settings))
      .catch((error) => this.error.set(errorMessage(error)));
  }

  setName(name: string) {
    this.draft.update((draft) => draft && { ...draft, name });
  }

  setBilling(billingEnabled: boolean) {
    this.draft.update((draft) => draft && { ...draft, billingEnabled, autoInvoice: billingEnabled && draft.autoInvoice });
  }

  setAutoInvoice(autoInvoice: boolean) {
    this.draft.update((draft) => draft && { ...draft, autoInvoice });
  }

  discard() {
    const saved = this.saved();
    if (saved) this.draft.set({ ...saved });
    this.error.set('');
  }

  async save() {
    const draft = this.draft();
    if (!draft || !this.dirty()) return;
    if (!draft.name.trim()) {
      this.tab.set(SettingsTab.General);
      this.error.set('Escribí el nombre de la clínica.');
      return;
    }
    this.error.set('');
    this.saving.set(true);
    try {
      const settings = await firstValueFrom(this.clinic.saveClinicSettings({ ...draft, name: draft.name.trim() }));
      this.reset(settings);
      clinicName.set(settings.name);
      this.toasts.success('Configuración guardada', 'Los cambios ya rigen para toda la clínica.');
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  private reset(settings: ClinicSettingsDto) {
    this.saved.set(settings);
    this.draft.set({ ...settings });
  }
}
