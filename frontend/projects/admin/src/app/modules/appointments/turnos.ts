import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './turnos.html',
  styles: `
    a {
      border-bottom: 2px solid transparent;
      padding-bottom: 0.5rem;
      color: rgb(23 51 44 / 0.7);
    }

    a.is-current {
      border-bottom-color: var(--color-clinic);
      color: var(--color-ink);
      font-weight: 700;
    }
  `,
})
export class TurnosPage {}
