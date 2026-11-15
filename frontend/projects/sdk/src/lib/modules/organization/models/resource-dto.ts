// A consulting room or a piece of equipment, both kept per site.
export interface RoomDto {
  id: string;
  locationId: string;
  location: string;
  name: string;
  isActive: boolean;
}

export interface EquipmentDto {
  id: string;
  locationId: string;
  location: string;
  name: string;
  isActive: boolean;
}

export interface SaveResourceCommand {
  locationId: string;
  name: string;
}
