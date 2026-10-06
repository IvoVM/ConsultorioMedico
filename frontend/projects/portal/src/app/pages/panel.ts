import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { clinicaSession } from 'sdk';

@Component({
  imports: [RouterLink],
  templateUrl: './panel.html',
})
export class PanelPage {
  readonly session = clinicaSession;
}
