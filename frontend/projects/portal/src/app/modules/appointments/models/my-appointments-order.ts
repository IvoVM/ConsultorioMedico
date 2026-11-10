// How the patient lines up their requests and appointments; newest created first unless they pick otherwise.
export enum MyAppointmentsOrder {
  Creado = 'creado',
  Cambio = 'cambio',
  Turno = 'turno',
}

export const myAppointmentsOrderLabels: Record<MyAppointmentsOrder, string> = {
  [MyAppointmentsOrder.Creado]: 'Último creado',
  [MyAppointmentsOrder.Cambio]: 'Último cambio',
  [MyAppointmentsOrder.Turno]: 'Fecha del turno',
};

// The patient sees their live appointments first, then the requests front desk still has to confirm,
// and apart from both, what left the flow: removed appointments and rejected requests.
export enum MyAppointmentsTab {
  Turnos = 'turnos',
  Solicitudes = 'solicitudes',
  Eliminados = 'eliminados',
}

export const myAppointmentsTabLabels: Record<MyAppointmentsTab, string> = {
  [MyAppointmentsTab.Turnos]: 'Turnos',
  [MyAppointmentsTab.Solicitudes]: 'Solicitudes pendientes',
  [MyAppointmentsTab.Eliminados]: 'Eliminados',
};

type Dated = { start: string; createdAt?: string | null; updatedAt?: string | null };

function moment(item: Dated, order: MyAppointmentsOrder) {
  const value = order === MyAppointmentsOrder.Turno ? item.start
    : order === MyAppointmentsOrder.Cambio ? (item.updatedAt ?? item.createdAt ?? item.start)
    : (item.createdAt ?? item.start);
  return new Date(value).getTime();
}

export function sortMine<T extends Dated>(items: T[], order: MyAppointmentsOrder) {
  // Bookings loaded together share a creation moment, so the later visit goes first among them.
  return [...items].sort((a, b) => moment(b, order) - moment(a, order) || new Date(b.start).getTime() - new Date(a.start).getTime());
}

export function matchesSearch(term: string, ...fields: (string | null | undefined)[]) {
  const needle = term.trim().toLowerCase();
  return !needle || fields.some((field) => field?.toLowerCase().includes(needle));
}
