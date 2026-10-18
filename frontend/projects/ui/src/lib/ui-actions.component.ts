import { AfterViewInit, Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { UiIcon } from './ui-icon.component';

@Component({
  selector: 'ui-actions',
  imports: [UiIcon],
  host: { '[class.is-many]': 'many()', '[class.is-open]': 'open()' },
  templateUrl: './ui-actions.component.html',
  styleUrl: './ui-actions.component.css',
})
export class UiActions implements AfterViewInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly panel = viewChild.required<ElementRef<HTMLElement>>('panel');
  private readonly trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
  readonly menuId = `ui-actions-${crypto.randomUUID()}`;
  readonly many = signal(false);
  readonly open = signal(false);

  ngAfterViewInit() {
    const node = this.panel().nativeElement;
    const sync = () => this.sync();
    const observer = new MutationObserver(sync);
    observer.observe(node, { childList: true, subtree: true });
    sync();
    this.destroyRef.onDestroy(() => {
      observer.disconnect();
      this.unbind();
    });
  }

  onToggle(event: Event) {
    const next = (event as ToggleEvent).newState === 'open';
    this.open.set(next);
    if (!next) {
      this.unbind();
      return;
    }
    this.bind();
    requestAnimationFrame(() => this.place());
  }

  close() {
    const panel = this.panel().nativeElement;
    if (panel.matches(':popover-open')) panel.hidePopover();
  }

  private sync() {
    const panel = this.panel().nativeElement;
    const count = panel.querySelectorAll(':scope > button, :scope > a, :scope > ui-button').length;
    const many = count > 1;
    this.many.set(many);
    if (!many) {
      this.open.set(false);
      if (panel.matches(':popover-open')) panel.hidePopover();
      panel.removeAttribute('popover');
      return;
    }
    panel.id = this.menuId;
    if (!panel.hasAttribute('popover')) panel.setAttribute('popover', 'auto');
  }

  private place() {
    const trigger = this.trigger().nativeElement.getBoundingClientRect();
    const panel = this.panel().nativeElement;
    const width = panel.offsetWidth;
    const height = panel.offsetHeight;
    let left = trigger.right - width;
    if (left < 8) left = 8;
    if (left + width > window.innerWidth - 8) left = Math.max(8, window.innerWidth - width - 8);
    const below = window.innerHeight - trigger.bottom;
    const top = below < height + 8 && trigger.top > height + 8 ? trigger.top - height - 4 : trigger.bottom + 4;
    panel.style.top = `${Math.max(8, top)}px`;
    panel.style.left = `${left}px`;
  }

  private readonly dismiss = () => this.close();

  private bind() {
    window.addEventListener('scroll', this.dismiss, true);
    window.addEventListener('resize', this.dismiss);
  }

  private unbind() {
    window.removeEventListener('scroll', this.dismiss, true);
    window.removeEventListener('resize', this.dismiss);
  }
}
