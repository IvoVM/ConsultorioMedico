// The appointment history keeps the bookings still in the flow apart from the ones removed from it.
export enum HistoryTab {
  Turnos = 'turnos',
  Eliminados = 'eliminados',
}

export const historyTabLabels: Record<HistoryTab, string> = {
  [HistoryTab.Turnos]: 'Turnos',
  [HistoryTab.Eliminados]: 'Eliminados',
};
