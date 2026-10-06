import { Component, input, output } from '@angular/core';

@Component({
  selector: 'ui-calendar',
  templateUrl: './ui-calendar.component.html',
})
export class UiCalendar {
  readonly fecha = input('');
  readonly fechaChange = output<string>();

  onChange(event: Event) {
    this.fechaChange.emit((event.target as HTMLInputElement).value);
  }
}
