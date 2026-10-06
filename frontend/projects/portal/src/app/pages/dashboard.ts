import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { clinicaSession, roleLabel } from 'sdk';

@Component({
  imports: [RouterLink],
  templateUrl: './dashboard.html',
})
export class DashboardPage {
  readonly session = clinicaSession;
  readonly roleLabel = roleLabel;
}
