import { Component, computed, input } from '@angular/core';

const tones = [
  ['#e3f6ea', '#14634b'],
  ['#fdf0dc', '#9a5208'],
  ['#e6effd', '#2459b8'],
  ['#fbe9ec', '#a8344a'],
  ['#efe9fb', '#5b3fb0'],
  ['#e5f5f6', '#16717a'],
] as const;

export function initials(name: string) {
  const parts = name
    .replace(/[^\p{L}\s]/gu, ' ')
    .split(/\s+/)
    .filter(Boolean);
  if (!parts.length) return '?';
  const first = parts[0][0];
  const last = parts.length > 1 ? parts[parts.length - 1][0] : (parts[0][1] ?? '');
  return (first + last).toUpperCase();
}

@Component({
  selector: 'ui-avatar',
  host: {
    class: 'ui-avatar',
    '[style.width]': 'size()',
    '[style.height]': 'size()',
    '[style.background]': 'tone()[0]',
    '[style.color]': 'tone()[1]',
    'aria-hidden': 'true',
  },
  template: `{{ letters() }}`,
  styles: `
    :host {
      display: inline-grid;
      flex-shrink: 0;
      place-items: center;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 700;
      letter-spacing: 0.02em;
      box-shadow: inset 0 0 0 1px rgb(14 42 34 / 0.06);
    }
  `,
})
export class UiAvatar {
  readonly name = input('');
  readonly size = input('2.25rem');
  readonly letters = computed(() => initials(this.name()));
  readonly tone = computed(() => {
    let hash = 0;
    for (const char of this.name()) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
    return tones[hash % tones.length];
  });
}
