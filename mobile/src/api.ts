import { sessionStore, type Session } from './session';

export const apiUrl = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5080';

export class ApiError extends Error {}

export type TokenDto = {
  token: string;
  email: string;
  name: string;
  role: string;
  tenantSlug?: string | null;
  mustChangePassword: boolean;
};

export type Clinic = { slug: string; name: string };
export type Location = { id: string; name: string; address: string; isActive: boolean };
export type Specialty = { id: string; name: string };
export type Service = { id: string; locationId: string; name: string };
export type AppointmentType = { id: string; name: string; durationMinutes: number; specialtyId?: string | null };
export type Professional = { id: string; firstName: string; lastName: string; email: string; licenseNumber?: string | null; specialtyId?: string | null };
export type Slot = {
  professionalId: string;
  professional: string;
  locationId: string;
  appointmentTypeId: string;
  appointmentType: string;
  start: string;
  end: string;
};
export type Appointment = {
  id: string;
  patientId: string;
  patient: string;
  professionalId: string;
  professional: string;
  locationId: string;
  location: string;
  appointmentTypeId: string;
  appointmentType: string;
  start: string;
  end: string;
  status: string;
  visitReason?: string | null;
};
export type WaitlistEntry = {
  id: string;
  patientId: string;
  patient: string;
  professionalId?: string | null;
  locationId: string;
  specialtyId: string;
  status: string;
  createdAt: string;
  notes?: string | null;
};
export type Diagnosis = { id: string; code: string; name: string };
export type Encounter = {
  id: string;
  appointmentId: string;
  patientId: string;
  professionalId: string;
  note?: string | null;
  isClosed: boolean;
  createdAt: string;
  diagnoses: Diagnosis[];
};
export type Prescription = {
  id: string;
  encounterId: string;
  patientId: string;
  professional: string;
  instructions?: string | null;
  createdAt: string;
  items: { medication: string; dose: string; frequency: string; duration: string }[];
};
export type History = {
  patient: { id: string; name: string; documentNumber: string; birthDate: string; phone: string };
  encounters: Encounter[];
  prescriptions: Prescription[];
};
export type PatientCard = {
  patientId: string;
  firstName: string;
  lastName: string;
  documentNumber: string;
  birthDate: string;
  phone: string;
};
export type MedicalRecord = {
  patientId: string;
  firstName: string;
  lastName: string;
  email: string;
  documentNumber: string;
  birthDate: string;
  phone: string;
  bloodType?: string | null;
  allergies?: string | null;
  personalHistory?: string | null;
  familyHistory?: string | null;
  currentMedication?: string | null;
  habits?: string | null;
  healthInsurance?: string | null;
  memberNumber?: string | null;
  emergencyContact?: string | null;
  emergencyPhone?: string | null;
  notes?: string | null;
};
export type Fee = { id: string; appointmentTypeId: string; appointmentType: string; amount: number; effectiveFrom: string };
export type Invoice = {
  id: string;
  appointmentId: string;
  patientId: string;
  patient: string;
  total: number;
  status: string;
  paymentMethod?: string | null;
  createdAt: string;
  paidAt?: string | null;
};
export type AuditEntry = { id: string; action: string; entity: string; detail?: string | null; timestamp: string };
export type ScheduleBlock = { id: string; day: string; startTime: string; endTime: string; locationId: string; appointmentTypeId: string };

let refreshing: Promise<string | null> | null = null;

async function message(response: Response) {
  try {
    const body = (await response.json()) as { error?: string };
    return body.error ?? 'No se pudo completar la operación.';
  } catch {
    return 'No se pudo completar la operación.';
  }
}

async function refreshAccess() {
  const current = sessionStore.get();
  if (!current?.refresh) return null;
  if (!refreshing) {
    refreshing = (async () => {
      const response = await fetch(`${apiUrl}/api/acceso/renovar`, {
        method: 'POST',
        headers: { 'X-Refresh-Token': current.refresh },
      });
      if (!response.ok) {
        await sessionStore.clear();
        return null;
      }
      const body = (await response.json()) as TokenDto;
      const refresh = response.headers.get('x-refresh-token') ?? current.refresh;
      await sessionStore.save({ token: body.token, refresh, role: body.role, name: body.name, email: body.email });
      return body.token;
    })().finally(() => {
      refreshing = null;
    });
  }
  return refreshing;
}

export async function request<T>(path: string, init: RequestInit = {}, auth = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  const token = auth ? sessionStore.get()?.token : null;
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let response = await fetch(`${apiUrl}${path}`, { ...init, headers });
  if (response.status === 401 && auth && token) {
    const next = await refreshAccess();
    if (next) {
      headers.set('Authorization', `Bearer ${next}`);
      response = await fetch(`${apiUrl}${path}`, { ...init, headers });
    }
  }
  if (!response.ok) throw new ApiError(await message(response));
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

async function sessionFrom(response: Response) {
  const body = (await response.json()) as TokenDto;
  if (!response.ok) throw new ApiError((body as { error?: string }).error ?? 'No se pudo ingresar.');
  const refresh = response.headers.get('x-refresh-token');
  if (!refresh) throw new ApiError('La API no devolvió la sesión. Reiniciá el servidor.');
  const session: Session = { token: body.token, refresh, role: body.role, name: body.name, email: body.email };
  await sessionStore.save(session);
  return session;
}

export async function login(email: string, password: string) {
  const response = await fetch(`${apiUrl}/api/acceso/ingreso`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  return sessionFrom(response);
}

export async function register(body: {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  documentNumber: string;
  birthDate: string;
  phone: string;
}) {
  const response = await fetch(`${apiUrl}/api/acceso/registro`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  return sessionFrom(response);
}

export async function logout() {
  const refresh = sessionStore.get()?.refresh;
  if (refresh) {
    await fetch(`${apiUrl}/api/acceso/salir`, { method: 'POST', headers: { 'X-Refresh-Token': refresh } }).catch(() => undefined);
  }
  await sessionStore.clear();
}

const query = (params: Record<string, string | undefined>) => {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) if (value) search.set(key, value);
  const text = search.toString();
  return text ? `?${text}` : '';
};

export const api = {
  clinic: () => request<Clinic>('/api/clinica', {}, false),
  locations: () => request<Location[]>('/api/sedes', {}, false),
  saveLocation: (body: { name: string; address: string }, id?: string) =>
    request<Location>(id ? `/api/sedes/${id}` : '/api/sedes', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  services: () => request<Service[]>('/api/servicios'),
  saveService: (body: { locationId: string; name: string }) => request<Service>('/api/servicios', { method: 'POST', body: JSON.stringify(body) }),
  specialties: () => request<Specialty[]>('/api/especialidades', {}, false),
  saveSpecialty: (body: { name: string }, id?: string) =>
    request<Specialty>(id ? `/api/especialidades/${id}` : '/api/especialidades', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  types: () => request<AppointmentType[]>('/api/tipos-turno', {}, false),
  saveType: (body: { name: string; durationMinutes: number; specialtyId?: string | null }, id?: string) =>
    request<AppointmentType>(id ? `/api/tipos-turno/${id}` : '/api/tipos-turno', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  professionals: (specialtyId?: string) => request<Professional[]>(`/api/profesionales${query({ specialtyId })}`, {}, false),
  assignSpecialty: (id: string, specialtyId: string | null) =>
    request<Professional>(`/api/profesionales/${id}/especialidad`, { method: 'PUT', body: JSON.stringify({ specialtyId }) }),
  importEmployees: (csv: string) => request<{ created: { email: string; temporaryPassword: string }[]; rejected: { row: number; reason: string }[] }>('/api/empleados/importar', { method: 'POST', body: JSON.stringify({ csv }) }),
  availability: (locationId: string, specialtyId: string, date: string, professionalId?: string) =>
    request<Slot[]>(`/api/disponibilidad${query({ locationId, specialtyId, date, professionalId })}`, {}, false),
  book: (body: { patientId?: string | null; professionalId: string; locationId: string; appointmentTypeId: string; start: string; visitReason?: string | null }) =>
    request<Appointment>('/api/turnos', { method: 'POST', body: JSON.stringify(body) }),
  day: (date: string, professionalId?: string) => request<Appointment[]>(`/api/turnos${query({ date, professionalId })}`),
  mine: () => request<Appointment[]>('/api/turnos/mios'),
  cancel: (id: string) => request<Appointment>(`/api/turnos/${id}/cancelar`, { method: 'POST' }),
  checkIn: (id: string) => request<Appointment>(`/api/turnos/${id}/admitir`, { method: 'POST' }),
  reschedule: (id: string, start: string) => request<Appointment>(`/api/turnos/${id}/reprogramar`, { method: 'POST', body: JSON.stringify({ start }) }),
  waitlist: () => request<WaitlistEntry[]>('/api/lista-espera'),
  joinWaitlist: (body: { patientId?: string | null; professionalId?: string | null; locationId: string; specialtyId: string; notes?: string | null }) =>
    request<WaitlistEntry>('/api/lista-espera', { method: 'POST', body: JSON.stringify(body) }),
  assignWaitlist: (id: string, start: string, appointmentTypeId: string) =>
    request<Appointment>(`/api/lista-espera/${id}/asignar`, { method: 'POST', body: JSON.stringify({ start, appointmentTypeId }) }),
  diagnoses: () => request<Diagnosis[]>('/api/diagnosticos'),
  saveEncounter: (body: { appointmentId: string; note?: string | null; bloodPressure?: string | null; heartRate?: number | null; temperature?: number | null; weightKg?: number | null; diagnosisIds: string[] }) =>
    request<Encounter>('/api/encuentros', { method: 'POST', body: JSON.stringify(body) }),
  closeEncounter: (id: string) => request<Encounter>(`/api/encuentros/${id}/cerrar`, { method: 'POST' }),
  prescribe: (body: { encounterId: string; instructions?: string | null; items: Prescription['items'] }) =>
    request<Prescription>('/api/recetas', { method: 'POST', body: JSON.stringify(body) }),
  myPrescriptions: () => request<Prescription[]>('/api/recetas/mias'),
  history: (patientId?: string) => request<History>(`/api/historia${query({ patientId })}`),
  searchPatients: (q: string) => request<PatientCard[]>(`/api/pacientes${query({ q })}`),
  createPatient: (body: { email: string; firstName: string; lastName: string; documentNumber: string; birthDate: string; phone: string; healthInsurance?: string; memberNumber?: string; emergencyContact?: string; emergencyPhone?: string }) =>
    request<{ patient: MedicalRecord; temporaryPassword: string }>('/api/pacientes', { method: 'POST', body: JSON.stringify(body) }),
  records: () => request<MedicalRecord[]>('/api/historias-medicas'),
  record: (patientId: string) => request<MedicalRecord>(`/api/historias-medicas/${patientId}`),
  saveRecord: (patientId: string, body: Omit<MedicalRecord, 'patientId' | 'email'>) =>
    request<MedicalRecord>(`/api/historias-medicas/${patientId}`, { method: 'PUT', body: JSON.stringify(body) }),
  schedule: (professionalId: string) => request<ScheduleBlock[]>(`/api/agendas${query({ professionalId })}`),
  saveSchedule: (professionalId: string, blocks: ScheduleBlock[]) =>
    request<ScheduleBlock[]>('/api/agendas', { method: 'PUT', body: JSON.stringify({ professionalId, blocks }) }),
  fees: () => request<Fee[]>('/api/aranceles'),
  createFee: (appointmentTypeId: string, amount: number, effectiveFrom: string) =>
    request<Fee>('/api/aranceles', { method: 'POST', body: JSON.stringify({ appointmentTypeId, amount, effectiveFrom }) }),
  invoices: () => request<Invoice[]>('/api/comprobantes'),
  pay: (id: string, method: 'Cash' | 'Transfer' | 'Card') =>
    request<Invoice>(`/api/comprobantes/${id}/pagar`, { method: 'POST', body: JSON.stringify({ method }) }),
  audit: () => request<AuditEntry[]>('/api/auditoria'),
};

export const statusLabel: Record<string, string> = {
  Booked: 'Reservado',
  CheckedIn: 'Admitido',
  InProgress: 'En atención',
  Completed: 'Atendido',
  Cancelled: 'Cancelado',
  NoShow: 'Ausente',
  Pending: 'En espera',
  Offered: 'Ofrecido',
  Accepted: 'Asignado',
};

export function todayIso() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

export function hora(iso: string) {
  return new Date(iso).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
}

export function fechaCorta(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short' });
}

export function diaLargo(date = new Date()) {
  return new Intl.DateTimeFormat('es-AR', { weekday: 'long', day: 'numeric', month: 'long' }).format(date);
}
