import { Component, input } from '@angular/core';

@Component({
  selector: 'ui-field',
  host: { class: 'block min-w-0' },
  templateUrl: './ui-field.component.html',
})
export class UiField {
  readonly label = input('');
}
