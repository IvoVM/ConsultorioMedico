import { Component, input, output } from '@angular/core';

@Component({
  selector: 'ui-calendar',
  templateUrl: './ui-calendar.component.html',
})
export class UiCalendar {
  readonly date = input('');
  readonly dateChange = output<string>();

  onChange(event: Event) {
    this.dateChange.emit((event.target as HTMLInputElement).value);
  }
}
