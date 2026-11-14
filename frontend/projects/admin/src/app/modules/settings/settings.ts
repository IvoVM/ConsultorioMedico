import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicService, ClinicSettingsDto, TaxCondition, errorMessage } from 'sdk';
import { SelectOption, TabItem, ToastService, UiButton, UiField, UiSelect, UiSkeleton, UiSwitch, UiTabs, UiHelp } from 'ui';
import { firstValueFrom } from 'rxjs';
import { clinicName } from '../../clinic-name';
import { SettingsTab, settingsTabLabels } from './models/settings-tab';
import { taxConditionLabels } from './models/tax-condition-labels';

@Component({
  imports: [UiHelp, FormsModule, UiButton, UiField, UiSelect, UiSkeleton, UiSwitch, UiTabs],
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
  readonly taxConditions: SelectOption[] = Object.values(TaxCondition).map((value) => ({ value, label: taxConditionLabels[value] }));
  readonly saved = signal<ClinicSettingsDto | null>(null);
  readonly draft = signal<ClinicSettingsDto | null>(null);
  readonly error = signal('');
  readonly saving = signal(false);
  // Nothing reaches the server until the admin saves, so the screen compares the draft with what is stored.
  readonly dirty = computed(() => {
    const saved = this.saved();
    const draft = this.draft();
    return !!saved && !!draft && (['name', 'billingEnabled', 'autoInvoice', 'fiscalEnabled', 'taxId', 'taxCondition', 'pointOfSale', 'autoAuthorize'] as const).some((key) => (draft[key] ?? null) !== (saved[key] ?? null));
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
    this.draft.update((draft) => draft && { ...draft, billingEnabled, autoInvoice: billingEnabled && draft.autoInvoice, fiscalEnabled: billingEnabled && draft.fiscalEnabled });
  }

  setAutoInvoice(autoInvoice: boolean) {
    this.draft.update((draft) => draft && { ...draft, autoInvoice });
  }

  setFiscal(fiscalEnabled: boolean) {
    this.draft.update((draft) => draft && { ...draft, fiscalEnabled, autoAuthorize: fiscalEnabled && !!draft.autoAuthorize });
  }

  setAutoAuthorize(autoAuthorize: boolean) {
    this.draft.update((draft) => draft && { ...draft, autoAuthorize });
  }

  setTaxId(taxId: string) {
    this.draft.update((draft) => draft && { ...draft, taxId: taxId.trim() || null });
  }

  setTaxCondition(taxCondition: string) {
    this.draft.update((draft) => draft && { ...draft, taxCondition: (taxCondition as TaxCondition) || null });
  }

  setPointOfSale(value: number | string | null) {
    const number = value === null || value === '' ? null : Number(value);
    this.draft.update((draft) => draft && { ...draft, pointOfSale: Number.isNaN(number) ? null : number });
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
