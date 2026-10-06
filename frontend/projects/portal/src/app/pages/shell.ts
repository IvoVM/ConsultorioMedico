import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { clinicaSession } from 'sdk';
import { UiButton, UiPage } from 'ui';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UiPage, UiButton],
  templateUrl: './shell.html',
})
export class ShellPage {
  readonly session = clinicaSession;
  logout() {
    clinicaSession.clear();
    location.href = '/ingreso';
  }
}
