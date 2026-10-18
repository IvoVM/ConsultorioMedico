export interface BookingRequestDto {
  id: string;
  name: string;
  phone: string;
  specialty: string;
  professional: string;
  appointmentType: string;
  start: string;
  insurer?: string | null;
  memberNumber?: string | null;
}

export interface CreateBookingRequestCommand {
  specialtyId: string;
  professionalId: string;
  locationId: string;
  appointmentTypeId: string;
  start: string;
  name: string;
  phone: string;
  insurerId?: string | null;
  memberNumber?: string | null;
}
