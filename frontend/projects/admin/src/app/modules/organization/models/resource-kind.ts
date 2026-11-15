// The two things a site has that appointments take one at a time.
export enum ResourceKind {
  Room = 'room',
  Equipment = 'equipment',
}

export const resourceKindLabels: Record<ResourceKind, { one: string; many: string }> = {
  [ResourceKind.Room]: { one: 'consultorio', many: 'Consultorios' },
  [ResourceKind.Equipment]: { one: 'equipo', many: 'Equipos' },
};
