import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PatientsService, errorMessage } from 'sdk';
import { UiButton, UiField, formatPhone } from 'ui';
import { firstValueFrom } from 'rxjs';
import { AppRoute } from '../../app.routes.enum';

export interface DeskPatient {
  documentNumber: string;
  firstName: string;
  lastName: string;
  phone: string;
}

@Component({
  imports: [FormsModule, UiButton, UiField],
  templateUrl: './desk-booking.html',
})
export class DeskBookingPage {
  private readonly patients = inject(PatientsService);
  private readonly router = inject(Router);
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
      await this.router.navigate([AppRoute.ReservarPaciente], { queryParams: { paciente: patient.patientId } });
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) this.unknown.set(true);
      else this.error.set(errorMessage(error));
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
    const draft: DeskPatient = {
      documentNumber: this.documentNumber.trim(),
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      phone,
    };
    await this.router.navigate([AppRoute.ReservarPaciente], { state: draft });
  }
}
