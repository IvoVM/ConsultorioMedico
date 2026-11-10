import type { AppointmentDto } from '../../appointments/models/appointment-dto';
import type { InsurerDto } from '../../organization/models/insurer-dto';

export interface AgendaRangeDto {
  start: string;
  end: string;
}

export interface AgendaTypeDto {
  id: string;
  name: string;
  durationMinutes: number;
}

export interface AgendaProfessionalDto {
  id: string;
  name: string;
  specialty?: string | null;
  ranges: AgendaRangeDto[];
  blockouts: AgendaRangeDto[];
  types: AgendaTypeDto[];
  insurers: InsurerDto[];
}

export interface AgendaDayDto {
  date: string;
  canEdit: boolean;
  professionals: AgendaProfessionalDto[];
  appointments: AppointmentDto[];
}

export interface MoveAppointmentCommand {
  professionalId: string;
  start: string;
}

export interface AgendaBookCommand {
  documentNumber: string;
  firstName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  professionalId: string;
  appointmentTypeId: string;
  start: string;
  insurerId?: string | null;
  memberNumber?: string | null;
}

export interface PlaceRequestCommand {
  professionalId: string;
  start: string;
}

export interface EditAppointmentCommand {
  insurerId?: string | null;
  memberNumber?: string | null;
}
