import { BookingRequestStatus } from './booking-request-status';

export interface BookingRequestDto {
  id: string;
  name: string;
  phone: string;
  documentNumber: string;
  specialty: string;
  professional: string;
  appointmentType: string;
  start: string;
  insurer?: string | null;
  memberNumber?: string | null;
  status: BookingRequestStatus;
}

export interface CreateBookingRequestCommand {
  specialtyId: string;
  professionalId: string;
  locationId: string;
  appointmentTypeId: string;
  start: string;
  name: string;
  phone: string;
  documentNumber: string;
  insurerId?: string | null;
  memberNumber?: string | null;
}
