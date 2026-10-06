export const organizationSections = [
  { id: 'servicios', label: 'Servicios' },
  { id: 'especialidades', label: 'Especialidades' },
  { id: 'tipos-de-turno', label: 'Tipos de turno' },
] as const;

export type OrganizationSection = (typeof organizationSections)[number]['id'];

export function isOrganizationSection(value: string | null): value is OrganizationSection {
  return organizationSections.some((section) => section.id === value);
}
