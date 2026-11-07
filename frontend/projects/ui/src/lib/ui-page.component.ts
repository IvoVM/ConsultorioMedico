import { Component, HostListener, input, output, signal } from '@angular/core';
import { UiAvatar } from './ui-avatar.component';
import { UiIcon } from './ui-icon.component';

@Component({
  selector: 'ui-page',
  imports: [UiAvatar, UiIcon],
  templateUrl: './ui-page.component.html',
})
export class UiPage {
  readonly eyebrow = input('Clínica');
  readonly brand = input('Clínica');
  readonly title = input('');
  readonly frame = input<'shell' | 'auth' | 'entry'>('entry');
  readonly userName = input('');
  readonly userRole = input('');
  readonly logout = output<void>();
  readonly menuOpen = signal(false);

  @HostListener('document:keydown.escape')
  closeMenu() {
    this.menuOpen.set(false);
  }

  onSidebarClick(event: Event) {
    const target = event.target as HTMLElement | null;
    if (target?.closest('a')) this.menuOpen.set(false);
  }
}
