import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EquipmentDto, LocationDto, OrganizationService, RoomDto, errorMessage } from 'sdk';
import { ToastService, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable } from 'ui';
import { Observable, firstValueFrom } from 'rxjs';
import { ResourceKind, resourceKindLabels } from './models/resource-kind';

type Editing = { kind: ResourceKind; id: string | null; name: string; locationId: string };

// The rooms and equipment of each site: the admin loads them, renames them and takes them out of use.
// Which room each professional works in is set on their weekly schedule; which equipment a practice needs, on the practice.
@Component({
  imports: [FormsModule, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './resources.html',
})
export class ResourcesPage {
  private readonly organization = inject(OrganizationService);
  private readonly toasts = inject(ToastService);
  readonly kinds = ResourceKind;
  readonly labels = resourceKindLabels;
  readonly locations = signal<LocationDto[]>([]);
  readonly rooms = signal<RoomDto[]>([]);
  readonly equipment = signal<EquipmentDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly editing = signal<Editing | null>(null);
  readonly saving = signal(false);
  readonly dialogError = signal('');
  readonly busy = signal('');
  readonly locationOptions = computed(() => this.locations().filter((item) => item.isActive).map((item) => ({ value: item.id, label: item.name })));

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const [locations, rooms, equipment] = await Promise.all([
        firstValueFrom(this.organization.locations()),
        firstValueFrom(this.organization.rooms()),
        firstValueFrom(this.organization.equipment()),
      ]);
      this.locations.set(locations);
      this.rooms.set(rooms);
      this.equipment.set(equipment);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  open(kind: ResourceKind, item?: RoomDto | EquipmentDto) {
    this.dialogError.set('');
    this.editing.set({
      kind,
      id: item?.id ?? null,
      name: item?.name ?? '',
      locationId: item?.locationId ?? this.locationOptions()[0]?.value ?? '',
    });
  }

  update(patch: Partial<Editing>) {
    this.editing.update((current) => current && { ...current, ...patch });
  }

  async save() {
    const editing = this.editing();
    if (!editing) return;
    const body = { locationId: editing.locationId, name: editing.name.trim() };
    if (!body.name) {
      this.dialogError.set(`El ${this.labels[editing.kind].one} necesita un nombre.`);
      return;
    }
    const room = editing.kind === ResourceKind.Room;
    const request: Observable<unknown> = editing.id
      ? room ? this.organization.updateRoom(editing.id, body) : this.organization.updateEquipment(editing.id, body)
      : room ? this.organization.createRoom(body) : this.organization.createEquipment(body);
    this.saving.set(true);
    this.dialogError.set('');
    try {
      await firstValueFrom(request);
      this.editing.set(null);
      await this.load();
    } catch (error) {
      this.dialogError.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  async toggle(kind: ResourceKind, item: RoomDto | EquipmentDto) {
    const request: Observable<unknown> =
      kind === ResourceKind.Room ? this.organization.setRoomActive(item.id, !item.isActive) : this.organization.setEquipmentActive(item.id, !item.isActive);
    this.busy.set(item.id);
    try {
      await firstValueFrom(request);
      await this.load();
    } catch (error) {
      this.toasts.error(`No se pudo ${item.isActive ? 'dar de baja' : 'reactivar'}`, errorMessage(error));
    } finally {
      this.busy.set('');
    }
  }
}
