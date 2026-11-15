export enum AppRoute {
  Ingreso = '/ingreso',
  Registro = '/registro',
  Clave = '/clave',
  Paciente = '/paciente',
  Reservar = '/reservar',
  Panel = '/panel',
  MisTurnos = '/mis-turnos',
  TurnosCancelados = '/turnos-cancelados',
  Historia = '/historia',
  Perfil = '/perfil',
  Recetas = '/recetas',
  MisComprobantes = '/mis-comprobantes',
  Caja = '/caja',
  Cuenta = '/cuenta',
  ReservarPaciente = '/reservar/paciente',
  DarTurno = '/dar-turno',
  Solicitudes = '/solicitudes',
  HistorialTurnos = '/historial-turnos',
  Mostrador = '/mostrador',
  Pacientes = '/pacientes',
  ListaEspera = '/lista-espera',
  Medico = '/medico',
  Agenda = '/agenda',
  Insumos = '/insumos',
}

export function routePath(route: AppRoute): string {
  return route.slice(1);
}

// Query parameters several screens pass to each other.
export enum RouteParam {
  Paciente = 'paciente',
  Fecha = 'fecha',
  Turno = 'turno',
}

export function patientParams(patientId: string) {
  return { [RouteParam.Paciente]: patientId };
}

// Opens the agenda on the appointment's day with its detail showing.
export function appointmentParams(appointment: { id: string; start: string }) {
  return { [RouteParam.Fecha]: appointment.start.slice(0, 10), [RouteParam.Turno]: appointment.id };
}
