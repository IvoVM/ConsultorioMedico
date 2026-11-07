import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'ui-badge',
  templateUrl: './ui-badge.component.html',
})
export class UiBadge {
  readonly tone = input<'ok' | 'warn' | 'muted'>('muted');
  readonly classes = computed(() => {
    const tone = {
      ok: 'bg-clinic-wash text-clinic-deep before:bg-leaf',
      warn: 'bg-ochre-wash text-ochre before:bg-ochre',
      muted: 'bg-mist text-ink/75 ring-1 ring-rule before:bg-ink/35',
    }[this.tone()];
    return `inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-0.5 text-xs font-semibold before:h-1.5 before:w-1.5 before:rounded-full before:content-[''] ${tone}`;
  });
}
