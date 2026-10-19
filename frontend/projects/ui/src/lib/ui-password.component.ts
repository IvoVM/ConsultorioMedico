import { AfterContentInit, Component, ElementRef, inject, signal } from '@angular/core';
import { UiIcon } from './ui-icon.component';

@Component({
  selector: 'ui-password',
  imports: [UiIcon],
  host: { class: 'relative block' },
  templateUrl: './ui-password.component.html',
})
export class UiPassword implements AfterContentInit {
  private readonly host = inject(ElementRef<HTMLElement>);
  readonly visible = signal(false);

  ngAfterContentInit() {
    const input = this.input();
    if (input) input.style.paddingRight = '2.75rem';
  }

  toggle() {
    const input = this.input();
    if (!input) return;
    this.visible.update((value) => !value);
    input.type = this.visible() ? 'text' : 'password';
  }

  private input() {
    return this.host.nativeElement.querySelector('input');
  }
}
