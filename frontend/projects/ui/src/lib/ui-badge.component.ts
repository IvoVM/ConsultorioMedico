import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'ui-badge',
  templateUrl: './ui-badge.component.html',
})
export class UiBadge {
  readonly tone = input<'ok' | 'warn' | 'muted'>('muted');
  readonly classes = computed(() => {
    const tone = {
      ok: 'bg-clinic-wash text-clinic-deep',
      warn: 'bg-ochre-wash text-ochre',
      muted: 'bg-mist text-ink/80',
    }[this.tone()];
    return `inline-flex rounded-sm px-2 py-0.5 text-xs font-bold ${tone}`;
  });
}
