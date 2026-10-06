import { Component, input } from '@angular/core';
import { UiIcon } from './ui-icon.component';

@Component({
  selector: 'ui-spinner',
  imports: [UiIcon],
  host: { class: 'inline-flex', 'aria-hidden': 'true' },
  templateUrl: './ui-spinner.component.html',
  styles: `
    :host {
      animation: ui-spin 0.8s linear infinite;
    }

    @keyframes ui-spin {
      to {
        transform: rotate(360deg);
      }
    }
  `,
})
export class UiSpinner {
  readonly size = input(18);
}
