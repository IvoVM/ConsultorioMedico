export function greeting(name: string | null | undefined, now = new Date()) {
  const hour = now.getHours();
  const salute = hour < 6 ? 'Buenas noches' : hour < 13 ? 'Buenos días' : hour < 20 ? 'Buenas tardes' : 'Buenas noches';
  const first = (name ?? '').trim().split(/\s+/)[0];
  return first ? `${salute}, ${first}` : salute;
}

export function todayLabel(now = new Date()) {
  return new Intl.DateTimeFormat('es-AR', { weekday: 'long', day: 'numeric', month: 'long' }).format(now);
}
