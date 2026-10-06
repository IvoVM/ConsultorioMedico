import { Component, HostListener, input, signal } from '@angular/core';

@Component({
  selector: 'ui-page',
  templateUrl: './ui-page.component.html',
})
export class UiPage {
  readonly eyebrow = input('Clínica');
  readonly title = input('');
  readonly frame = input<'shell' | 'auth' | 'entry'>('entry');
  readonly menuOpen = signal(false);

  @HostListener('document:keydown.escape')
  closeMenu() {
    this.menuOpen.set(false);
  }

  onSidebarClick(event: Event) {
    const target = event.target as HTMLElement | null;
    if (target?.closest('a, button')) this.menuOpen.set(false);
  }
}
