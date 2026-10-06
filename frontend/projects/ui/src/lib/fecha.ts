import { Pipe, PipeTransform } from '@angular/core';

const dateTime = new Intl.DateTimeFormat('es-AR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

const dateOnly = new Intl.DateTimeFormat('es-AR', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
});

export function formatFecha(value?: string | null, mode: 'fecha' | 'hora' = 'fecha'): string {
  if (!value) return '';
  const text = value.trim();
  const clock = text.match(/^(\d{2}):(\d{2})/);
  if (clock && !text.includes('-')) return `${clock[1]}:${clock[2]}`;

  const match = text.match(/^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2}))?/);
  if (!match) {
    const parsed = new Date(text);
    return Number.isNaN(parsed.getTime()) ? text : dateTime.format(parsed);
  }

  const hour = match[4];
  const minute = match[5];
  if (mode === 'hora' && hour && minute) return `${hour}:${minute}`;

  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), hour ? Number(hour) : 12, minute ? Number(minute) : 0);
  return hour ? dateTime.format(date) : dateOnly.format(date);
}

@Pipe({ name: 'fecha' })
export class FechaPipe implements PipeTransform {
  transform(value?: string | null, mode: 'fecha' | 'hora' = 'fecha') {
    return formatFecha(value, mode);
  }
}
