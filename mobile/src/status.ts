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

// Which account to enter with when one document holds both a staff and a patient account.
export enum LoginAccount {
  Staff = 'Staff',
  Patient = 'Patient',
}
