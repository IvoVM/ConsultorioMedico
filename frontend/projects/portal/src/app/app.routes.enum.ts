export enum AppRoute {
  Ingreso = '/ingreso',
  IngresoEmpleado = '/ingreso/empleado',
  IngresoCuenta = '/ingreso/cuenta',
  Registro = '/registro',
  Clave = '/clave',
  Paciente = '/paciente',
  Reservar = '/reservar',
  Panel = '/panel',
  MisTurnos = '/mis-turnos',
  TurnosEliminados = '/turnos-eliminados',
  Historia = '/historia',
  Perfil = '/perfil',
  Recetas = '/recetas',
  ReservarPaciente = '/reservar/paciente',
  DarTurno = '/dar-turno',
  Solicitudes = '/solicitudes',
  HistorialTurnos = '/historial-turnos',
  Secretaria = '/secretaria',
  Medico = '/medico',
  Agenda = '/agenda',
}

export function routePath(route: AppRoute): string {
  return route.slice(1);
}

// Query parameters several screens pass to each other.
export enum RouteParam {
  Paciente = 'paciente',
}

export function patientParams(patientId: string) {
  return { [RouteParam.Paciente]: patientId };
}
