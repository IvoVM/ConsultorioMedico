export enum RequestTab {
  Vigentes = 'vigentes',
  Vencidas = 'vencidas',
}

export function startOfToday(now = new Date()) {
  const today = new Date(now);
  today.setHours(0, 0, 0, 0);
  return today;
}

export function localDate(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function requestExpired(start: string, now = new Date()) {
  return new Date(start) < startOfToday(now);
}

export function daysOverdue(start: string, now = new Date()) {
  const day = new Date(start);
  day.setHours(0, 0, 0, 0);
  return Math.round((startOfToday(now).getTime() - day.getTime()) / 86_400_000);
}

export const requestTabLabels: Record<RequestTab, string> = {
  [RequestTab.Vigentes]: 'Por confirmar',
  [RequestTab.Vencidas]: 'Vencidas',
};

export function overdueLabel(start: string, now = new Date()) {
  const days = daysOverdue(start, now);
  return days === 1 ? 'Venció ayer' : `Venció hace ${days} días`;
}

export const expiredRequestDays = 30;

export function requestDeactivated(start: string, now = new Date()) {
  return daysOverdue(start, now) >= expiredRequestDays;
}

export function deactivationLabel(start: string, now = new Date()) {
  const left = expiredRequestDays - daysOverdue(start, now);
  return left === 1 ? 'Se desactiva mañana' : `Se desactiva en ${left} días`;
}
