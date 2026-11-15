// What each column of the agenda is: a professional, a consulting room or a piece of equipment.
export enum AgendaView {
  Professional = 'professional',
  Room = 'room',
  Equipment = 'equipment',
}

export const agendaViewLabels: Record<AgendaView, string> = {
  [AgendaView.Professional]: 'Profesionales',
  [AgendaView.Room]: 'Consultorios',
  [AgendaView.Equipment]: 'Equipos',
};

// The room column that holds the appointments with no room yet (clinics that never set rooms up).
export const NO_ROOM = 'sin-consultorio';
