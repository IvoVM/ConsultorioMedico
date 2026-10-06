import { GuardarHistoriaMedicaCommand, HistoriaMedicaDto } from 'sdk';

export const GRUPOS_SANGUINEOS = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', '0+', '0-'];

export type FichaMedica = { [K in keyof GuardarHistoriaMedicaCommand]-?: string };

export function aFicha(historia: HistoriaMedicaDto): FichaMedica {
  return {
    nombre: historia.nombre,
    apellido: historia.apellido,
    documento: historia.documento,
    fechaNacimiento: historia.fechaNacimiento,
    telefono: historia.telefono,
    grupoSanguineo: historia.grupoSanguineo ?? '',
    alergias: historia.alergias ?? '',
    antecedentesPersonales: historia.antecedentesPersonales ?? '',
    antecedentesFamiliares: historia.antecedentesFamiliares ?? '',
    medicacionHabitual: historia.medicacionHabitual ?? '',
    habitos: historia.habitos ?? '',
    obraSocial: historia.obraSocial ?? '',
    numeroAfiliado: historia.numeroAfiliado ?? '',
    contactoEmergencia: historia.contactoEmergencia ?? '',
    telefonoEmergencia: historia.telefonoEmergencia ?? '',
    observaciones: historia.observaciones ?? '',
  };
}

export function edad(fechaNacimiento: string) {
  const nacimiento = new Date(`${fechaNacimiento}T00:00:00`);
  if (Number.isNaN(nacimiento.getTime())) return null;
  const hoy = new Date();
  let anios = hoy.getFullYear() - nacimiento.getFullYear();
  const mes = hoy.getMonth() - nacimiento.getMonth();
  if (mes < 0 || (mes === 0 && hoy.getDate() < nacimiento.getDate())) anios--;
  return anios;
}

export function fechaCorta(valor?: string | null) {
  if (!valor) return null;
  return new Intl.DateTimeFormat('es-AR', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(valor));
}
