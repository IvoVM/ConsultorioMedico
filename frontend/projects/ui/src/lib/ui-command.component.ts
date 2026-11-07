import { Component, ElementRef, HostListener, computed, inject, input, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { UiIcon } from './ui-icon.component';
import type { NavNode } from './ui-nav.component';

type Target = { label: string; group: string; path: string; icon: string };

function plain(text: string) {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
}

@Component({
  selector: 'ui-command',
  imports: [UiIcon],
  host: { class: 'ui-command' },
  templateUrl: './ui-command.component.html',
  styleUrl: './ui-command.component.css',
})
export class UiCommand {
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly field = viewChild.required<ElementRef<HTMLInputElement>>('field');
  readonly nodes = input<NavNode[]>([]);
  readonly placeholder = input('Buscar pantallas…');
  readonly query = signal('');
  readonly open = signal(false);
  readonly active = signal(0);
  readonly shortcut = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.platform) ? '⌘K' : 'Ctrl K';

  private readonly targets = computed((): Target[] =>
    this.nodes().flatMap((node) => {
      const icon = node.icon ?? 'arrow_forward';
      if (node.children?.length) return node.children.map((child) => ({ label: child.label, group: node.label, path: child.path, icon: child.icon ?? icon }));
      return node.path ? [{ label: node.label, group: '', path: node.path, icon }] : [];
    }),
  );

  readonly results = computed(() => {
    const words = plain(this.query()).split(/\s+/).filter(Boolean);
    const all = this.targets();
    if (!words.length) return all.slice(0, 8);
    return all.filter((target) => {
      const haystack = plain(`${target.label} ${target.group}`);
      return words.every((word) => haystack.includes(word));
    });
  });

  @HostListener('document:keydown', ['$event'])
  onShortcut(event: KeyboardEvent) {
    if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.field().nativeElement.focus();
      this.field().nativeElement.select();
      this.open.set(true);
    }
  }

  @HostListener('document:pointerdown', ['$event'])
  onOutside(event: Event) {
    if (!this.host.nativeElement.contains(event.target as Node)) this.open.set(false);
  }

  onInput(value: string) {
    this.query.set(value);
    this.active.set(0);
    this.open.set(true);
  }

  onKey(event: KeyboardEvent) {
    const count = this.results().length;
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.open.set(true);
      if (count) this.active.update((index) => (index + 1) % count);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (count) this.active.update((index) => (index - 1 + count) % count);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const target = this.results()[this.active()];
      if (target) this.go(target);
    } else if (event.key === 'Escape') {
      this.open.set(false);
      this.field().nativeElement.blur();
    }
  }

  go(target: Target) {
    void this.router.navigateByUrl(target.path);
    this.query.set('');
    this.open.set(false);
    this.field().nativeElement.blur();
  }
}
