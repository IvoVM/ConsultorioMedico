export const organizationSections = [
  { id: 'servicios', label: 'Servicios' },
] as const;

export type OrganizationSection = (typeof organizationSections)[number]['id'];

export function isOrganizationSection(value: string | null): value is OrganizationSection {
  return organizationSections.some((section) => section.id === value);
}
