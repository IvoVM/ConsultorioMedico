import { Component, input, output } from '@angular/core';

@Component({
  selector: 'ui-modal',
  templateUrl: './ui-modal.component.html',
  styles: `
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
export class UiModal {
  readonly open = input(false);
  readonly title = input('');
  readonly closed = output<void>();
}
