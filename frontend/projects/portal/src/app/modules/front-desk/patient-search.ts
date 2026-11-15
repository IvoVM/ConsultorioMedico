import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PatientLookupDto, PatientsService, errorMessage } from 'sdk';
import { UiEmpty, UiField, UiHelp } from 'ui';
import { firstValueFrom } from 'rxjs';
import { AppRoute, patientParams } from '../../app.routes.enum';

// Finding a patient when the phone rings or someone asks at the desk, with the way to their bookings and account.
@Component({
  imports: [FormsModule, RouterLink, UiEmpty, UiField, UiHelp],
  templateUrl: './patient-search.html',
})
export class PatientSearchPage {
  private readonly patientsApi = inject(PatientsService);
  private searchSeq = 0;
  private searchTimer = 0;
  readonly route = AppRoute;
  readonly patientParams = patientParams;
  readonly searchError = signal('');
  readonly searching = signal(false);
  readonly matches = signal<PatientLookupDto[]>([]);
  query = '';

  onQuery(value: string) {
    this.query = value;
    window.clearTimeout(this.searchTimer);
    this.searchTimer = window.setTimeout(() => void this.search(), 250);
  }

  age(birthDate: string) {
    const birth = new Date(`${birthDate}T00:00:00`);
    if (Number.isNaN(birth.getTime()) || birth.getFullYear() < 1901) return null;
    const today = new Date();
    let years = today.getFullYear() - birth.getFullYear();
    const months = today.getMonth() - birth.getMonth();
    if (months < 0 || (months === 0 && today.getDate() < birth.getDate())) years--;
    return years;
  }

  private async search() {
    const q = this.query.trim();
    const seq = ++this.searchSeq;
    if (q.length < 2) {
      this.matches.set([]);
      this.searchError.set('');
      this.searching.set(false);
      return;
    }
    this.searching.set(true);
    this.searchError.set('');
    try {
      const matches = await firstValueFrom(this.patientsApi.searchPatients({ q }));
      if (seq !== this.searchSeq) return;
      this.matches.set(matches);
    } catch (error) {
      if (seq !== this.searchSeq) return;
      this.matches.set([]);
      this.searchError.set(errorMessage(error));
    } finally {
      if (seq === this.searchSeq) this.searching.set(false);
    }
  }
}
