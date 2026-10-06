/* Código generado por scripts/generate-sdk.mjs a partir de openapi/clinica.json. No editar a mano. */
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { CLINICA_API_URL } from './tokens';

export interface LoginCommand {
  email: string;
  password: string;
}

export interface TokenDto {
  token: string;
  email: string;
  nombre: string;
  rol: string;
  tenantSlug?: string | null;
  debeCambiarClave: boolean;
}

export interface AltaTenantCommand {
  slug: string;
  nombre: string;
  tipo: string;
}

export interface CambiarEstadoTenantCommand {
  estado: string;
}

export interface TenantDto {
  id: string;
  slug: string;
  nombre: string;
  tipo: string;
  estado: string;
  creadoEn: string;
}

export interface RegistroPacienteCommand {
  email: string;
  password: string;
  nombre: string;
  apellido: string;
  documento: string;
  fechaNacimiento: string;
  telefono: string;
}

export interface SedeDto {
  id: string;
  nombre: string;
  direccion: string;
  activa: boolean;
}

export interface GuardarSedeCommand {
  nombre: string;
  direccion: string;
}

export interface ServicioDto {
  id: string;
  sedeId: string;
  nombre: string;
}

export interface GuardarServicioCommand {
  sedeId: string;
  nombre: string;
}

export interface EspecialidadDto {
  id: string;
  nombre: string;
}

export interface GuardarEspecialidadCommand {
  nombre: string;
}

export interface TipoTurnoDto {
  id: string;
  nombre: string;
  duracionMinutos: number;
  especialidadId?: string | null;
}

export interface GuardarTipoTurnoCommand {
  nombre: string;
  duracionMinutos: number;
  especialidadId?: string | null;
}

export interface ProfesionalDto {
  id: string;
  nombre: string;
  apellido: string;
  email: string;
  matricula?: string | null;
  especialidadId?: string | null;
}

export interface ImportarEmpleadosCommand {
  csv: string;
}

export interface EmpleadoCreadoDto {
  fila: number;
  email: string;
  claveTemporal: string;
  rol: string;
}

export interface FilaRechazadaDto {
  fila: number;
  motivo: string;
}

export interface ImportacionEmpleadosDto {
  creados: EmpleadoCreadoDto[];
  rechazados: FilaRechazadaDto[];
}

export interface BloqueAgendaDto {
  id: string;
  dia: string;
  horaDesde: string;
  horaHasta: string;
  sedeId: string;
  tipoTurnoId: string;
}

export interface GuardarAgendaCommand {
  profesionalId: string;
  bloques: BloqueAgendaDto[];
}

export interface BloqueoDto {
  id: string;
  profesionalId: string;
  inicio: string;
  fin: string;
  motivo: string;
}

export interface CrearBloqueoCommand {
  profesionalId: string;
  inicio: string;
  fin: string;
  motivo: string;
}

export interface HuecoDto {
  profesionalId: string;
  profesional: string;
  sedeId: string;
  tipoTurnoId: string;
  tipoTurno: string;
  inicio: string;
  fin: string;
}

export interface ReservarTurnoCommand {
  pacienteId?: string | null;
  profesionalId: string;
  sedeId: string;
  tipoTurnoId: string;
  inicio: string;
  motivoConsulta?: string | null;
}

export interface TurnoDto {
  id: string;
  pacienteId: string;
  paciente: string;
  profesionalId: string;
  profesional: string;
  sedeId: string;
  sede: string;
  tipoTurnoId: string;
  tipoTurno: string;
  inicio: string;
  fin: string;
  estado: string;
  motivoConsulta?: string | null;
}

export interface ReprogramarTurnoCommand {
  inicio: string;
}

export interface ListaEsperaDto {
  id: string;
  pacienteId: string;
  paciente: string;
  profesionalId?: string | null;
  sedeId: string;
  especialidadId: string;
  estado: string;
  creadoEn: string;
  notas?: string | null;
}

export interface CrearListaEsperaCommand {
  pacienteId?: string | null;
  profesionalId?: string | null;
  sedeId: string;
  especialidadId: string;
  notas?: string | null;
}

export interface AsignarListaEsperaCommand {
  inicio: string;
  tipoTurnoId: string;
}

export interface DiagnosticoDto {
  id: string;
  codigo: string;
  nombre: string;
}

export interface GuardarEncuentroCommand {
  turnoId: string;
  nota?: string | null;
  tensionArterial?: string | null;
  frecuenciaCardiaca?: number | null;
  temperatura?: number | null;
  pesoKg?: number | null;
  diagnosticoIds: string[];
}

export interface EncuentroDto {
  id: string;
  turnoId: string;
  pacienteId: string;
  profesionalId: string;
  nota?: string | null;
  tensionArterial?: string | null;
  frecuenciaCardiaca?: number | null;
  temperatura?: number | null;
  pesoKg?: number | null;
  cerrado: boolean;
  creadoEn: string;
  diagnosticos: DiagnosticoDto[];
}

export interface RecetaItemDto {
  medicamento: string;
  dosis: string;
  frecuencia: string;
  duracion: string;
}

export interface CrearRecetaCommand {
  encuentroId: string;
  indicaciones?: string | null;
  items: RecetaItemDto[];
}

export interface RecetaDto {
  id: string;
  encuentroId: string;
  pacienteId: string;
  profesionalId: string;
  profesional: string;
  indicaciones?: string | null;
  creadoEn: string;
  items: RecetaItemDto[];
}

export interface PacienteResumenDto {
  id: string;
  nombre: string;
  documento: string;
  fechaNacimiento: string;
  telefono: string;
}

export interface HistoriaDto {
  paciente: PacienteResumenDto;
  encuentros: EncuentroDto[];
  recetas: RecetaDto[];
}

export interface HistoriaMedicaDto {
  pacienteId: string;
  nombre: string;
  apellido: string;
  email: string;
  documento: string;
  fechaNacimiento: string;
  telefono: string;
  grupoSanguineo?: string | null;
  alergias?: string | null;
  antecedentesPersonales?: string | null;
  antecedentesFamiliares?: string | null;
  medicacionHabitual?: string | null;
  habitos?: string | null;
  obraSocial?: string | null;
  numeroAfiliado?: string | null;
  contactoEmergencia?: string | null;
  telefonoEmergencia?: string | null;
  observaciones?: string | null;
  actualizadoEn?: string | null;
}

export interface GuardarHistoriaMedicaCommand {
  nombre: string;
  apellido: string;
  documento: string;
  fechaNacimiento: string;
  telefono: string;
  grupoSanguineo?: string | null;
  alergias?: string | null;
  antecedentesPersonales?: string | null;
  antecedentesFamiliares?: string | null;
  medicacionHabitual?: string | null;
  habitos?: string | null;
  obraSocial?: string | null;
  numeroAfiliado?: string | null;
  contactoEmergencia?: string | null;
  telefonoEmergencia?: string | null;
  observaciones?: string | null;
}

export interface ArancelDto {
  id: string;
  tipoTurnoId: string;
  tipoTurno: string;
  monto: number;
  vigenteDesde: string;
}

export interface CrearArancelCommand {
  tipoTurnoId: string;
  monto: number;
  vigenteDesde: string;
}

export interface ComprobanteItemDto {
  descripcion: string;
  importe: number;
}

export interface ComprobanteDto {
  id: string;
  turnoId: string;
  pacienteId: string;
  paciente: string;
  total: number;
  estado: string;
  metodoPago?: string | null;
  creadoEn: string;
  pagadoEn?: string | null;
  items: ComprobanteItemDto[];
}

export interface PagarComprobanteCommand {
  metodo: string;
}

export interface AuditoriaDto {
  id: string;
  usuarioId?: string | null;
  accion: string;
  entidad: string;
  entidadId?: string | null;
  detalle?: string | null;
  fecha: string;
}

@Injectable({ providedIn: 'root' })
export class ClinicaClient {
  private readonly http = inject(HttpClient);
  private readonly base = inject(CLINICA_API_URL);

  private queryString(query?: Record<string, unknown>) {
    if (!query) return '';
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') params.set(key, String(value));
    }
    const text = params.toString();
    return text ? `?${text}` : '';
  }

  loginPlataforma(body: LoginCommand) {
    return this.http.post<TokenDto>(`${this.base}/api/platform/auth/login`, body);
  }

  listarTenants() {
    return this.http.get<TenantDto[]>(`${this.base}/api/platform/tenants`);
  }

  crearTenant(body: AltaTenantCommand) {
    return this.http.post<TenantDto>(`${this.base}/api/platform/tenants`, body);
  }

  cambiarEstadoTenant(id: string, body: CambiarEstadoTenantCommand) {
    return this.http.patch<TenantDto>(`${this.base}/api/platform/tenants/${id}/estado`, body);
  }

  loginTenant(body: LoginCommand) {
    return this.http.post<TokenDto>(`${this.base}/api/auth/login`, body);
  }

  registrarPaciente(body: RegistroPacienteCommand) {
    return this.http.post<TokenDto>(`${this.base}/api/auth/registro`, body);
  }

  sedes() {
    return this.http.get<SedeDto[]>(`${this.base}/api/sedes`);
  }

  crearSede(body: GuardarSedeCommand) {
    return this.http.post<SedeDto>(`${this.base}/api/sedes`, body);
  }

  actualizarSede(id: string, body: GuardarSedeCommand) {
    return this.http.put<SedeDto>(`${this.base}/api/sedes/${id}`, body);
  }

  servicios() {
    return this.http.get<ServicioDto[]>(`${this.base}/api/servicios`);
  }

  crearServicio(body: GuardarServicioCommand) {
    return this.http.post<ServicioDto>(`${this.base}/api/servicios`, body);
  }

  especialidades() {
    return this.http.get<EspecialidadDto[]>(`${this.base}/api/especialidades`);
  }

  crearEspecialidad(body: GuardarEspecialidadCommand) {
    return this.http.post<EspecialidadDto>(`${this.base}/api/especialidades`, body);
  }

  tiposTurno() {
    return this.http.get<TipoTurnoDto[]>(`${this.base}/api/tipos-turno`);
  }

  crearTipoTurno(body: GuardarTipoTurnoCommand) {
    return this.http.post<TipoTurnoDto>(`${this.base}/api/tipos-turno`, body);
  }

  profesionales(query?: { especialidadId?: string }) {
    return this.http.get<ProfesionalDto[]>(`${this.base}/api/profesionales` + this.queryString(query));
  }

  importarEmpleados(body: ImportarEmpleadosCommand) {
    return this.http.post<ImportacionEmpleadosDto>(`${this.base}/api/empleados/importar`, body);
  }

  obtenerAgenda(query: { profesionalId: string }) {
    return this.http.get<BloqueAgendaDto[]>(`${this.base}/api/agendas` + this.queryString(query));
  }

  guardarAgenda(body: GuardarAgendaCommand) {
    return this.http.put<BloqueAgendaDto[]>(`${this.base}/api/agendas`, body);
  }

  bloqueos(query: { profesionalId: string }) {
    return this.http.get<BloqueoDto[]>(`${this.base}/api/bloqueos` + this.queryString(query));
  }

  crearBloqueo(body: CrearBloqueoCommand) {
    return this.http.post<BloqueoDto>(`${this.base}/api/bloqueos`, body);
  }

  eliminarBloqueo(id: string) {
    return this.http.delete<void>(`${this.base}/api/bloqueos/${id}`);
  }

  disponibilidad(query: { sedeId: string; especialidadId: string; profesionalId?: string; fecha: string }) {
    return this.http.get<HuecoDto[]>(`${this.base}/api/disponibilidad` + this.queryString(query));
  }

  turnosDelDia(query: { fecha: string; profesionalId?: string }) {
    return this.http.get<TurnoDto[]>(`${this.base}/api/turnos` + this.queryString(query));
  }

  reservarTurno(body: ReservarTurnoCommand) {
    return this.http.post<TurnoDto>(`${this.base}/api/turnos`, body);
  }

  misTurnos() {
    return this.http.get<TurnoDto[]>(`${this.base}/api/turnos/mios`);
  }

  cancelarTurno(id: string) {
    return this.http.post<TurnoDto>(`${this.base}/api/turnos/${id}/cancelar`, {});
  }

  admitirTurno(id: string) {
    return this.http.post<TurnoDto>(`${this.base}/api/turnos/${id}/admitir`, {});
  }

  reprogramarTurno(id: string, body: ReprogramarTurnoCommand) {
    return this.http.post<TurnoDto>(`${this.base}/api/turnos/${id}/reprogramar`, body);
  }

  listaEspera() {
    return this.http.get<ListaEsperaDto[]>(`${this.base}/api/lista-espera`);
  }

  anotarEspera(body: CrearListaEsperaCommand) {
    return this.http.post<ListaEsperaDto>(`${this.base}/api/lista-espera`, body);
  }

  asignarEspera(id: string, body: AsignarListaEsperaCommand) {
    return this.http.post<TurnoDto>(`${this.base}/api/lista-espera/${id}/asignar`, body);
  }

  diagnosticos() {
    return this.http.get<DiagnosticoDto[]>(`${this.base}/api/diagnosticos`);
  }

  guardarEncuentro(body: GuardarEncuentroCommand) {
    return this.http.post<EncuentroDto>(`${this.base}/api/encuentros`, body);
  }

  cerrarEncuentro(id: string) {
    return this.http.post<EncuentroDto>(`${this.base}/api/encuentros/${id}/cerrar`, {});
  }

  crearReceta(body: CrearRecetaCommand) {
    return this.http.post<RecetaDto>(`${this.base}/api/recetas`, body);
  }

  misRecetas() {
    return this.http.get<RecetaDto[]>(`${this.base}/api/recetas/mias`);
  }

  obtenerReceta(id: string) {
    return this.http.get<RecetaDto>(`${this.base}/api/recetas/${id}`);
  }

  historia(query?: { pacienteId?: string }) {
    return this.http.get<HistoriaDto>(`${this.base}/api/historia` + this.queryString(query));
  }

  historiasMedicas() {
    return this.http.get<HistoriaMedicaDto[]>(`${this.base}/api/historias-medicas`);
  }

  historiaMedica(pacienteId: string) {
    return this.http.get<HistoriaMedicaDto>(`${this.base}/api/historias-medicas/${pacienteId}`);
  }

  guardarHistoriaMedica(pacienteId: string, body: GuardarHistoriaMedicaCommand) {
    return this.http.put<HistoriaMedicaDto>(`${this.base}/api/historias-medicas/${pacienteId}`, body);
  }

  aranceles() {
    return this.http.get<ArancelDto[]>(`${this.base}/api/aranceles`);
  }

  crearArancel(body: CrearArancelCommand) {
    return this.http.post<ArancelDto>(`${this.base}/api/aranceles`, body);
  }

  comprobantes() {
    return this.http.get<ComprobanteDto[]>(`${this.base}/api/comprobantes`);
  }

  pagarComprobante(id: string, body: PagarComprobanteCommand) {
    return this.http.post<ComprobanteDto>(`${this.base}/api/comprobantes/${id}/pagar`, body);
  }

  auditoria() {
    return this.http.get<AuditoriaDto[]>(`${this.base}/api/auditoria`);
  }
}
