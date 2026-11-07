import { Component, DestroyRef, HostListener, inject, input, output, signal } from '@angular/core';
import { UiAvatar } from './ui-avatar.component';
import { UiCommand } from './ui-command.component';
import { UiIcon } from './ui-icon.component';
import type { NavNode } from './ui-nav.component';

const clockFormat = new Intl.DateTimeFormat('es-AR', { hour: '2-digit', minute: '2-digit' });

@Component({
  selector: 'ui-page',
  imports: [UiAvatar, UiCommand, UiIcon],
  templateUrl: './ui-page.component.html',
})
export class UiPage {
  readonly eyebrow = input('Clínica');
  readonly brand = input('Clínica');
  readonly title = input('');
  readonly frame = input<'shell' | 'auth' | 'entry'>('entry');
  readonly nodes = input<NavNode[]>([]);
  readonly userName = input('');
  readonly userRole = input('');
  readonly logout = output<void>();
  readonly menuOpen = signal(false);
  readonly clock = signal(clockFormat.format(new Date()));

  constructor() {
    const timer = setInterval(() => this.clock.set(clockFormat.format(new Date())), 15_000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  @HostListener('document:keydown.escape')
  closeMenu() {
    this.menuOpen.set(false);
  }

  onSidebarClick(event: Event) {
    const target = event.target as HTMLElement | null;
    if (target?.closest('a')) this.menuOpen.set(false);
  }
}
