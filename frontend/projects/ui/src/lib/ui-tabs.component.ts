import { Component, input, model } from '@angular/core';
import { UiIcon } from './ui-icon.component';
import { UiTone } from './ui-board.component';

export type TabItem = {
  key: string;
  label: string;
  icon?: string;
  count?: number;
  tone?: UiTone;
};

@Component({
  selector: 'ui-tabs',
  imports: [UiIcon],
  template: `
    <div class="tabs" role="tablist" [attr.aria-label]="label()">
      @for (tab of tabs(); track tab.key) {
        <button
          type="button"
          role="tab"
          class="tab"
          [class.is-active]="tab.key === active()"
          [attr.aria-selected]="tab.key === active()"
          (click)="active.set(tab.key)"
        >
          @if (tab.icon) {
            <ui-icon [name]="tab.icon" [size]="18" />
          }
          <span>{{ tab.label }}</span>
          @if (tab.count !== undefined) {
            <span class="count" [attr.data-tone]="tab.count ? tab.tone : null">{{ tab.count }}</span>
          }
        </button>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      max-width: 100%;
      overflow-x: auto;
    }

    .tabs {
      display: inline-flex;
      gap: 0.25rem;
      border-radius: 999px;
      background: white;
      padding: 0.3rem;
      box-shadow: var(--shadow-card);
    }

    .tab {
      position: relative;
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      border-radius: 999px;
      padding: 0.5rem 1rem;
      font-size: 0.875rem;
      font-weight: 600;
      white-space: nowrap;
      color: rgb(14 42 34 / 0.6);
      cursor: pointer;
      transition: background-color 0.3s cubic-bezier(0.16, 0.84, 0.32, 1), color 0.2s ease, transform 0.25s ease;
    }

    .tab:hover:not(.is-active) {
      background: var(--color-mist);
      color: var(--color-ink);
    }

    .tab.is-active {
      background: var(--color-forest);
      color: white;
      box-shadow: 0 8px 18px -10px rgb(15 59 47 / 0.8);
      animation: tab-in 0.35s cubic-bezier(0.16, 0.84, 0.32, 1);
    }

    .count {
      min-width: 1.4rem;
      border-radius: 999px;
      background: var(--color-mist);
      padding: 0.05rem 0.45rem;
      font-size: 0.75rem;
      text-align: center;
      color: rgb(14 42 34 / 0.65);
    }

    .count[data-tone='warn'] {
      background: var(--color-ochre-wash);
      color: var(--color-ochre);
    }

    .count[data-tone='bad'] {
      background: var(--color-pulse-wash);
      color: var(--color-pulse-deep);
    }

    .tab.is-active .count {
      background: rgb(255 255 255 / 0.18);
      color: white;
    }

    @keyframes tab-in {
      from { transform: scale(0.94); }
      to { transform: none; }
    }
  `,
})
export class UiTabs {
  readonly tabs = input<TabItem[]>([]);
  readonly label = input('Pestañas');
  readonly active = model('');
}
