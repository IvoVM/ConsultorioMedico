import { Component, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter, map } from 'rxjs';
import { UiIcon } from './ui-icon.component';

export type NavLink = {
  label: string;
  path: string;
  exact?: boolean;
  icon?: string;
};

export type NavNode = {
  label: string;
  path?: string;
  exact?: boolean;
  icon?: string;
  children?: NavLink[];
};

@Component({
  selector: 'ui-nav',
  imports: [RouterLink, RouterLinkActive, UiIcon],
  host: { class: 'nav-scroll block min-h-0' },
  templateUrl: './ui-nav.component.html',
  styles: `
    :host {
      overflow-y: auto;
      scrollbar-width: thin;
      scrollbar-color: rgb(143 208 180 / 0.5) transparent;
    }

    .group-body {
      display: grid;
      grid-template-rows: 1fr;
      animation: group-open 0.35s cubic-bezier(0.16, 0.84, 0.32, 1) both;
    }

    @keyframes group-open {
      from {
        grid-template-rows: 0fr;
        opacity: 0;
      }
      to {
        grid-template-rows: 1fr;
        opacity: 1;
      }
    }

    :host::-webkit-scrollbar {
      width: 6px;
    }

    :host::-webkit-scrollbar-track {
      background: transparent;
    }

    :host::-webkit-scrollbar-thumb {
      border-radius: 999px;
      background: rgb(143 208 180 / 0.45);
    }

    :host::-webkit-scrollbar-thumb:hover {
      background: rgb(143 208 180 / 0.8);
    }

    :host::-webkit-scrollbar-button {
      display: none;
      width: 0;
      height: 0;
    }
  `,
})
export class UiNav {
  private readonly router = inject(Router);
  readonly nodes = input.required<NavNode[]>();
  readonly label = input('Menú');
  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );
  private readonly opened = signal<ReadonlySet<string>>(new Set());
  private readonly collapsed = signal<string | null>(null);

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.collapsed.set(null));
  }

  isOpen(node: NavNode) {
    if (this.collapsed() === node.label) return false;
    if (this.containsCurrent(node)) return true;
    return this.opened().has(node.label);
  }

  containsCurrent(node: NavNode) {
    return (node.children ?? []).some((child) => this.matches(child));
  }

  toggle(node: NavNode) {
    if (this.containsCurrent(node)) {
      this.collapsed.update((current) => (current === node.label ? null : node.label));
      return;
    }
    this.opened.update((current) => {
      const next = new Set(current);
      if (next.has(node.label)) next.delete(node.label);
      else next.add(node.label);
      return next;
    });
  }

  private matches(link: NavLink) {
    const path = (this.url() ?? '').split(/[?#]/)[0];
    if (link.exact) return path === link.path;
    return path === link.path || path.startsWith(`${link.path}/`);
  }
}
