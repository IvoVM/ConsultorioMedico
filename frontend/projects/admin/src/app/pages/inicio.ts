import { Component } from '@angular/core';
import { clinicaSession } from 'sdk';

@Component({
  templateUrl: './inicio.html',
})
export class InicioPage {
  readonly session = clinicaSession;
}
