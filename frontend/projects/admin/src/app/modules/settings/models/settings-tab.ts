// The settings screen splits what the admin can change into tabs.
export enum SettingsTab {
  General = 'general',
  Facturacion = 'facturacion',
}

export const settingsTabLabels: Record<SettingsTab, string> = {
  [SettingsTab.General]: 'General',
  [SettingsTab.Facturacion]: 'Facturación',
};
