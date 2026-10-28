import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { UiToasts } from 'ui';

@Component({
  imports: [RouterOutlet, UiToasts],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('admin');
}
