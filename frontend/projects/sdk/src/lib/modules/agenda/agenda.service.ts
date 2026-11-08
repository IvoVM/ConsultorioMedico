import { Injectable } from '@angular/core';
import { ApiClient } from '../../core/api-client';
import type { AppointmentDto } from '../appointments/models/appointment-dto';
import type { AgendaBookCommand, AgendaDayDto, EditAppointmentCommand, MoveAppointmentCommand, PlaceRequestCommand } from './models/agenda-day-dto';

@Injectable({ providedIn: 'root' })
export class AgendaService extends ApiClient {
  day(date: string) {
    return this.http.get<AgendaDayDto>(`${this.base}/api/agenda?date=${date}`);
  }

  book(body: AgendaBookCommand) {
    return this.http.post<AppointmentDto>(`${this.base}/api/agenda/turnos`, body);
  }

  move(id: string, body: MoveAppointmentCommand) {
    return this.http.post<AppointmentDto>(`${this.base}/api/agenda/turnos/${id}/mover`, body);
  }

  edit(id: string, body: EditAppointmentCommand) {
    return this.http.put<AppointmentDto>(`${this.base}/api/agenda/turnos/${id}`, body);
  }

  placeRequest(id: string, body: PlaceRequestCommand) {
    return this.http.post<AppointmentDto>(`${this.base}/api/agenda/solicitudes/${id}/ubicar`, body);
  }
}
