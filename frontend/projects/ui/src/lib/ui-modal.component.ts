import { Component, input, output } from '@angular/core';

@Component({
  selector: 'ui-modal',
  templateUrl: './ui-modal.component.html',
})
export class UiModal {
  readonly open = input(false);
  readonly title = input('');
  readonly closed = output<void>();
}
