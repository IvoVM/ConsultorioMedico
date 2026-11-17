import type { AppointmentDto } from '../../appointments/models/appointment-dto';
import type { InsurerDto } from '../../organization/models/insurer-dto';
import type { PracticeSupplyDto } from '../../organization/models/practice-dto';

export interface AgendaRangeDto {
  start: string;
  end: string;
}

export interface AgendaTypeDto {
  id: string;
  name: string;
  durationMinutes: number;
  code?: string | null;
  equipment?: string | null;
  supplies: PracticeSupplyDto[];
}

// A column of the agenda when it is shown by room or by equipment.
export interface AgendaResourceDto {
  id: string;
  name: string;
  locationId: string;
  location: string;
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
  rooms: AgendaResourceDto[];
  equipment: AgendaResourceDto[];
  maxOverbooksPerDay?: number | null;
}

export interface MoveAppointmentCommand {
  professionalId: string;
  start: string;
  roomId?: string | null;
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
  patientInformed?: boolean;
}

export interface PlaceRequestCommand {
  professionalId: string;
  start: string;
}

export interface EditAppointmentCommand {
  insurerId?: string | null;
  memberNumber?: string | null;
}
