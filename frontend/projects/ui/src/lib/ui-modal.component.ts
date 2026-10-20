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
      color: var(--color-clinic-deep);
      font-size: 1.75rem;
      line-height: 1;
      cursor: pointer;
    }

    .modal-close:hover {
      background: var(--color-clinic-wash);
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
