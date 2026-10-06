import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClinicService } from 'sdk';
import { readClinicName } from './clinic-name';

@Component({
  imports: [RouterLink],
  templateUrl: './patient-entry.html',
})
export class PatientEntryPage {
  private readonly clinic = inject(ClinicService);
  readonly clinicName = signal('Consultorio');

  constructor() {
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
  }
}
