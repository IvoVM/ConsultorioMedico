import { Component } from '@angular/core';
import { clinicaSession } from 'sdk';

@Component({
  templateUrl: './home.html',
})
export class HomePage {
  readonly session = clinicaSession;
}
