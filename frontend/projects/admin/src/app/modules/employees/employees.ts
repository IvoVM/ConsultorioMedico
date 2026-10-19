import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreatedEmployeeDto,
  EmployeeDto,
  EmployeesService,
  InsurerDto,
  OrganizationService,
  RejectedRowDto,
  TenantRole,
  errorMessage,
} from 'sdk';
import { SelectOption, UiActions, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';
import { ScreenNote } from '../../screen-note';
import { TablePager, listParams, queueSearch } from '../../table-pager';
import { SortCol, SortState, nextSort } from '../../table-sort';
import { roleLabels } from './models/role-labels';

export const sampleCsv = `nombre,apellido,documento,email,rol,matricula,especialidad
Camila,Suárez,40111222,camila.suarez@prueba.local,Medico,MN 55110,Clínica médica
Pedro,Luna,40222333,pedro.luna@prueba.local,Secretario,,
Inés,Molina,40333444,ines.molina@prueba.local,AdminTenant,,
,soloapellido,12,correo-invalido,Ayudante,,
`;

const csvRoles: Record<string, string> = {
  medico: 'Médico',
  médico: 'Médico',
  secretario: 'Secretario',
  admintenant: 'Administrador',
};

interface PreviewRow {
  row: number;
  name: string;
  email: string;
  role: string;
  ok: boolean;
  reason: string;
}

@Component({
  imports: [FormsModule, ScreenNote, SortCol, TablePager, UiActions, UiBadge, UiButton, UiEmpty, UiField, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './employees.html',
})
export class EmployeesPage {
  private readonly employees = inject(EmployeesService);
  private readonly organization = inject(OrganizationService);
  readonly roleLabels = roleLabels;
  readonly roles = TenantRole;
  readonly roleOptions: SelectOption[] = [
    { value: TenantRole.Doctor, label: 'Médico' },
    { value: TenantRole.Secretary, label: 'Secretario' },
    { value: TenantRole.TenantAdmin, label: 'Administrador' },
  ];
  readonly people = signal<EmployeeDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly specialties = signal<SelectOption[]>([]);
  readonly insurers = signal<InsurerDto[]>([]);
  readonly insurerIds = signal<string[]>([]);
  readonly created = signal<CreatedEmployeeDto[]>([]);
  readonly rejected = signal<RejectedRowDto[]>([]);
  readonly preview = signal<PreviewRow[]>([]);
  readonly fileName = signal('');
  readonly error = signal('');
  readonly password = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly panel = signal<'cargar' | 'importar' | 'editar' | ''>('');
  readonly editing = signal<EmployeeDto | null>(null);
  readonly statusTarget = signal<EmployeeDto | null>(null);
  readonly query = signal('');
  readonly sort = signal<SortState>({ key: 'name', dir: 'asc' });
  private readonly searchHost: { timer?: ReturnType<typeof setTimeout> } = {};
  readonly csv = signal('');
  firstName = '';
  lastName = '';
  email = '';
  documentNumber = '';
  role: TenantRole = TenantRole.Doctor;
  licenseNumber = '';
  specialtyId = '';

  readonly specialtyOptions = computed(() => this.specialties());

  constructor() {
    void this.load();
  }

  async load() {
    try {
      const [people, specialties, insurers] = await Promise.all([
        firstValueFrom(this.employees.employeesPage(listParams(this.page(), this.sort(), this.query()))),
        firstValueFrom(this.organization.specialties()),
        firstValueFrom(this.organization.enabledInsurers()),
      ]);
      this.people.set(people.items);
      this.total.set(people.total);
      this.page.set(people.page);
      this.insurers.set(insurers);
      this.specialties.set(specialties.map((item) => ({ value: item.id, label: item.name })));
      this.specialtyId ||= specialties[0]?.id ?? '';
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  sortBy(key: string) {
    this.sort.update((current) => nextSort(current, key));
    this.page.set(1);
    void this.load();
  }

  onSearch(value: string) {
    this.query.set(value);
    this.page.set(1);
    queueSearch(this.searchHost, () => void this.load());
  }

  go(page: number) {
    this.page.set(page);
    void this.load();
  }

  open(panel: 'cargar' | 'importar') {
    this.error.set('');
    this.editing.set(null);
    if (panel === 'cargar') {
      this.firstName = '';
      this.lastName = '';
      this.email = '';
      this.documentNumber = '';
      this.role = TenantRole.Doctor;
      this.licenseNumber = '';
      this.specialtyId = this.specialties()[0]?.value ?? '';
      this.insurerIds.set([]);
    }
    this.panel.set(panel);
  }

  toggleInsurer(id: string, checked: boolean) {
    this.insurerIds.update((current) => {
      if (checked) return current.includes(id) ? current : [...current, id];
      return current.filter((item) => item !== id);
    });
  }

  edit(person: EmployeeDto) {
    this.error.set('');
    this.editing.set(person);
    this.firstName = person.firstName;
    this.lastName = person.lastName;
    this.email = person.email;
    this.documentNumber = person.documentNumber ?? '';
    this.role = person.role;
    this.licenseNumber = person.licenseNumber ?? '';
    this.specialtyId = person.specialtyId ?? this.specialties()[0]?.value ?? '';
    this.insurerIds.set([...(person.insurerIds ?? [])]);
    this.panel.set('editar');
  }

  askStatus(person: EmployeeDto) {
    this.error.set('');
    this.statusTarget.set(person);
  }

  downloadSample() {
    const blob = new Blob([sampleCsv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'empleados-prueba.csv';
    link.click();
    URL.revokeObjectURL(url);
  }

  readFile(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.fileName.set(file.name);
    this.created.set([]);
    this.rejected.set([]);
    void file.text().then((text) => {
      this.csv.set(text);
      this.preview.set(previewCsv(text));
    });
  }

  formBody() {
    return {
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      email: this.email.trim(),
      documentNumber: this.documentNumber.trim(),
      role: this.role,
      licenseNumber: this.role === TenantRole.Doctor ? this.licenseNumber.trim() || null : null,
      specialtyId: this.role === TenantRole.Doctor ? this.specialtyId : null,
      insurerIds: this.role === TenantRole.Doctor ? this.insurerIds() : [],
    };
  }

  async create() {
    this.error.set('');
    this.password.set('');
    if (this.role === TenantRole.Doctor && !this.specialtyId) {
      this.error.set('Un médico necesita una especialidad. Cargala en Especialidades antes de esta ficha.');
      return;
    }
    this.saving.set(true);
    try {
      const current = this.editing();
      if (current) {
        await firstValueFrom(this.employees.updateEmployee(current.id, this.formBody()));
        this.editing.set(null);
      } else {
        const result = await firstValueFrom(this.employees.createEmployee(this.formBody()));
        this.password.set(result.temporaryPassword);
      }
      this.panel.set('');
      this.firstName = '';
      this.lastName = '';
      this.email = '';
      this.documentNumber = '';
      this.licenseNumber = '';
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  async confirmStatus() {
    const person = this.statusTarget();
    if (!person) return;
    this.error.set('');
    this.saving.set(true);
    try {
      await firstValueFrom(this.employees.setEmployeeActive(person.id, !person.active));
      this.statusTarget.set(null);
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  async import() {
    this.error.set('');
    this.saving.set(true);
    try {
      const result = await firstValueFrom(this.employees.importEmployees({ csv: this.csv() }));
      this.created.set(result.created);
      this.rejected.set(result.rejected);
      this.panel.set('');
      await this.load();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}

function previewCsv(text: string): PreviewRow[] {
  const lines = text.trim().replace(/^\uFEFF/, '').split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
  return lines.slice(1).map((line, index) => {
    const cols = line.split(',');
    const name = `${cols[0]?.trim() ?? ''} ${cols[1]?.trim() ?? ''}`.trim();
    const email = cols[3]?.trim() ?? '';
    const documentNumber = cols[2]?.trim() ?? '';
    const role = cols[4]?.trim() ?? '';
    const specialty = cols[6]?.trim() ?? '';
    const row = index + 2;
    if (cols.length < 5) {
      return bad(row, name, email, role, 'Faltan columnas.');
    }
    if (!cols[0]?.trim() || !cols[1]?.trim() || !email) {
      return bad(row, name, email, role, 'Nombre, apellido y email son obligatorios.');
    }
    const document = documentNumber.replace(/[.\-\s]/g, '');
    if (document.length < 6 || document.length > 20) {
      return bad(row, name, email, role, 'El documento tiene que tener entre 6 y 20 caracteres.');
    }
    if (!email.includes('@') || email.startsWith('@') || email.endsWith('@')) {
      return bad(row, name, email, role, 'El email no es válido.');
    }
    if (!csvRoles[role.toLowerCase()]) {
      return bad(row, name, email, role, 'El rol tiene que ser Medico, Secretario o AdminTenant.');
    }
    if (csvRoles[role.toLowerCase()] === 'Médico' && !specialty) {
      return bad(row, name, email, role, 'Un médico necesita una especialidad ya cargada.');
    }
    return { row, name, email, role: csvRoles[role.toLowerCase()], ok: true, reason: 'Se va a cargar.' };
  });
}

function bad(row: number, name: string, email: string, role: string, reason: string): PreviewRow {
  return { row, name: name || '—', email: email || '—', role: role || '—', ok: false, reason: `Esta fila no se carga: ${reason}` };
}
