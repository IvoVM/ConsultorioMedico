// The appointment history keeps the bookings still in the flow apart from the ones removed from it.
export enum HistoryTab {
  Turnos = 'turnos',
  Eliminados = 'eliminados',
}

// A clinic with one site has nothing to tell apart, so the history names it the same way everywhere.
export const singleSiteLabel = 'Sede única';

export function siteLabel(location: string, multiSite: boolean) {
  return multiSite && location ? location : singleSiteLabel;
}

export const historyTabLabels: Record<HistoryTab, string> = {
  [HistoryTab.Turnos]: 'Turnos',
  [HistoryTab.Eliminados]: 'Eliminados',
};
