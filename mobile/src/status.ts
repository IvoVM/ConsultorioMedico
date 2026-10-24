export enum AuthFrom {
  Solicitud = 'solicitud',
}

export enum BookingRequestStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Rescheduled = 'Rescheduled',
}

export enum AppointmentStatus {
  Booked = 'Booked',
  CheckedIn = 'CheckedIn',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow = 'NoShow',
  Rescheduled = 'Rescheduled',
}
