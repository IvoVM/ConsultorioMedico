import { AfterViewInit, Component, ElementRef, OnDestroy, booleanAttribute, inject, input, output } from '@angular/core';

@Component({
  selector: 'ui-modal',
  templateUrl: './ui-modal.component.html',
  styles: `
    :host {
      display: contents;
    }

    .modal-close {
      display: grid;
      width: 2.25rem;
      height: 2.25rem;
      flex-shrink: 0;
      place-items: center;
      border-radius: 999px;
      background: var(--color-mist);
      color: var(--color-ink);
      font-size: 1.5rem;
      line-height: 1;
      cursor: pointer;
      transition: background-color 0.2s ease, transform 0.3s cubic-bezier(0.16, 0.84, 0.32, 1);
    }

    .modal-close:hover {
      background: var(--color-clinic-wash);
      transform: rotate(90deg);
    }
  `,
})
export class UiModal implements AfterViewInit, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  readonly open = input(false);
  readonly title = input('');
  readonly wide = input(false, { transform: booleanAttribute });
  readonly closed = output<void>();

  ngAfterViewInit() {
    document.body.appendChild(this.host.nativeElement);
  }

  ngOnDestroy() {
    this.host.nativeElement.remove();
  }
}
