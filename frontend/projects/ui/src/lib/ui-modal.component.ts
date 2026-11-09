import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, booleanAttribute, effect, inject, input, output, signal } from '@angular/core';
import { UiIcon } from './ui-icon.component';

@Component({
  selector: 'ui-modal',
  imports: [UiIcon],
  templateUrl: './ui-modal.component.html',
  styleUrl: './ui-modal.component.css',
})
export class UiModal implements AfterViewInit, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  readonly open = input(false);
  readonly title = input('');
  readonly wide = input(false, { transform: booleanAttribute });
  readonly closed = output<void>();
  readonly scrolled = signal(false);
  readonly atEnd = signal(true);
  private observer?: ResizeObserver;

  constructor() {
    effect(() => {
      if (!this.open()) return;
      queueMicrotask(() => this.measure());
    });
  }

  ngAfterViewInit() {
    document.body.appendChild(this.host.nativeElement);
    this.observer = new ResizeObserver(() => this.measure());
    this.observer.observe(this.host.nativeElement);
  }

  ngOnDestroy() {
    this.observer?.disconnect();
    this.host.nativeElement.remove();
  }

  // Escape closes only the modal on top; a select or other control that handled the key first wins.
  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event) {
    if (!this.open() || event.defaultPrevented) return;
    const dialogs = document.querySelectorAll('ui-modal .modal-dialog');
    if (dialogs[dialogs.length - 1] !== this.host.nativeElement.querySelector('.modal-dialog')) return;
    event.preventDefault();
    this.closed.emit();
  }

  onScroll(event: Event) {
    this.read(event.target as HTMLElement);
  }

  private measure() {
    const body = this.host.nativeElement.querySelector('.modal-body') as HTMLElement | null;
    if (!body) return;
    this.observer?.observe(body);
    if (body.firstElementChild) this.observer?.observe(body.firstElementChild);
    this.read(body);
  }

  private read(body: HTMLElement) {
    this.scrolled.set(body.scrollTop > 2);
    this.atEnd.set(body.scrollTop + body.clientHeight >= body.scrollHeight - 2);
  }
}
