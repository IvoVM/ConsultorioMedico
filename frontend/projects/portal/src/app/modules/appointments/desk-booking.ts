import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClinicService, PatientsService, PrivacyNoticeDto, errorMessage } from 'sdk';
import { UiButton, UiConsent, UiField, formatPhone, UiHelp, staffConsentLabel } from 'ui';
import { firstValueFrom } from 'rxjs';
import { AppRoute, patientParams } from '../../app.routes.enum';

export interface DeskPatient {
  documentNumber: string;
  firstName: string;
  lastName: string;
  phone: string;
  // Front desk told the new patient the privacy notice before loading their data.
  patientInformed: boolean;
}

@Component({
  imports: [UiHelp, FormsModule, UiButton, UiConsent, UiField],
  templateUrl: './desk-booking.html',
})
export class DeskBookingPage {
  private readonly patients = inject(PatientsService);
  private readonly router = inject(Router);
  private readonly clinic = inject(ClinicService);
  readonly notice = signal<PrivacyNoticeDto | null>(null);
  readonly informed = signal(false);
  readonly consentLabel = staffConsentLabel;
  readonly error = signal('');
  readonly looking = signal(false);
  readonly unknown = signal(false);
  documentNumber = '';
  firstName = '';
  lastName = '';
  phone = '';

  async lookup() {
    this.error.set('');
    this.unknown.set(false);
    this.looking.set(true);
    try {
      const patient = await firstValueFrom(this.patients.patientByDocument(this.documentNumber.trim()));
      await this.router.navigate([AppRoute.ReservarPaciente], { queryParams: patientParams(patient.patientId) });
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.unknown.set(true);
        if (!this.notice()) this.clinic.privacyNotice().subscribe({ next: (notice) => this.notice.set(notice) });
      } else {
        this.error.set(errorMessage(error));
      }
    } finally {
      this.looking.set(false);
    }
  }

  async continueNew() {
    const phone = formatPhone(this.phone);
    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.error.set('Nombre y apellido son obligatorios.');
      return;
    }
    if (!phone) {
      this.error.set('El teléfono no es válido.');
      return;
    }
    if (!this.informed()) {
      this.error.set('Contale al paciente el aviso de privacidad y marcá que lo hiciste.');
      return;
    }
    const draft: DeskPatient = {
      documentNumber: this.documentNumber.trim(),
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      phone,
      patientInformed: true,
    };
    await this.router.navigate([AppRoute.ReservarPaciente], { state: draft });
  }
}
