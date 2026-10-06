import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { clinicaSession } from 'sdk';
import { roleLabel } from './models/role-labels';

@Component({
  imports: [RouterLink],
  templateUrl: './dashboard.html',
})
export class DashboardPage {
  readonly session = clinicaSession;
  readonly roleLabel = roleLabel;
}
