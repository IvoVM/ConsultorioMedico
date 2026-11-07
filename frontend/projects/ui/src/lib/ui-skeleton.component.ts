import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'ui-skeleton',
  host: { role: 'status', '[attr.aria-label]': 'label()' },
  templateUrl: './ui-skeleton.component.html',
  styles: `
    :host {
      display: block;
    }

    .stack {
      display: flex;
      flex-direction: column;
    }

    .stack.is-framed {
      gap: 1px;
      overflow: hidden;
      border-radius: 1.25rem;
      background: var(--color-rule, #e1eae5);
      box-shadow: var(--shadow-card);
    }

    .stack:not(.is-framed) {
      gap: 0.75rem;
    }

    .stack:not(.is-framed) .bar {
      border-radius: 0.85rem;
    }

    .bar {
      display: block;
      background-color: white;
      background-image: linear-gradient(90deg, transparent, rgb(220 243 229 / 0.9), transparent);
      background-size: 200% 100%;
      background-repeat: no-repeat;
      animation: shimmer 1.3s ease-in-out infinite;
    }

    .bar.is-circle {
      border-radius: 999px;
    }

    @keyframes shimmer {
      from {
        background-position: 150% 0;
      }
      to {
        background-position: -50% 0;
      }
    }
  `,
})
export class UiSkeleton {
  readonly count = input(4);
  readonly appearance = input<'line' | 'circle'>('line');
  readonly height = input('4.25rem');
  readonly width = input<string | null>(null);
  readonly framed = input(true);
  readonly label = input('Cargando');
  readonly rows = computed(() => Array.from({ length: Math.max(1, this.count()) }, (_, index) => index));
}
