import { Component, input } from '@angular/core';

@Component({
  selector: 'ui-empty',
  templateUrl: './ui-empty.component.html',
  styleUrl: './ui-empty.component.css',
})
export class UiEmpty {
  readonly message = input('No hay datos para mostrar.');
}
