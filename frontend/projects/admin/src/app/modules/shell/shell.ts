import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService, ClinicService, TenantRole, clinicaSession } from 'sdk';
import { firstValueFrom } from 'rxjs';
import { clinicName } from '../../clinic-name';
import { NavLink, NavNode, UiNav, UiPage } from 'ui';
import { roleLabels } from '../employees/models/role-labels';

@Component({
  imports: [RouterOutlet, UiPage, UiNav],
  templateUrl: './shell.html',
})
export class ShellPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  readonly session = clinicaSession;
  readonly clinicName = clinicName;
  readonly roleLabel = computed(() => {
    const role = this.session.role();
    return role ? roleLabels[role] : 'Administración';
  });
  // Front desk only reaches the appointment screens; everything else in the admin stays with the clinic admin.
  readonly menu = computed((): NavNode[] => (this.session.role() === TenantRole.Secretary ? this.deskMenu : this.adminMenu));
  private readonly home: NavLink = { label: 'Inicio', path: '/inicio', exact: true, icon: 'monitor_heart' };
  private readonly turnos: NavLink[] = [
    { label: 'Solicitudes de turnos', path: '/turnos/solicitudes', icon: 'pending_actions' },
    { label: 'Historial de turnos', path: '/turnos/historial', icon: 'history' },
  ];
  private readonly deskMenu: NavNode[] = [this.home, { label: 'Agenda', icon: 'calendar_month', children: this.turnos }];
  private readonly adminMenu: NavNode[] = [
      this.home,
      {
        label: 'Agenda',
        icon: 'calendar_month',
        children: [...this.turnos, { label: 'Agendas', path: '/agendas', icon: 'event_note' }],
      },
      {
        label: 'Pacientes',
        icon: 'group',
        children: [
          { label: 'Pacientes', path: '/pacientes', icon: 'person_search' },
          { label: 'Historias médicas', path: '/historias', icon: 'clinical_notes' },
        ],
      },
      {
        label: 'Atención',
        icon: 'stethoscope',
        children: [
          { label: 'Especialidades', path: '/organizacion/especialidades', icon: 'medical_services' },
          { label: 'Servicios', path: '/organizacion/servicios', icon: 'vaccines' },
        ],
      },
      {
        label: 'Facturación',
        icon: 'receipt_long',
        children: [
          { label: 'Obras sociales', path: '/organizacion/obras-sociales', icon: 'health_and_safety' },
          { label: 'Comprobantes', path: '/comprobantes', icon: 'request_quote' },
          { label: 'Cajas', path: '/cajas', icon: 'point_of_sale' },
          { label: 'Conciliación de tarjetas', path: '/conciliacion', icon: 'credit_card' },
        ],
      },
      {
        label: 'Personal',
        icon: 'badge',
        children: [
          { label: 'Empleados', path: '/empleados', icon: 'id_card' },
          { label: 'Usuarios', path: '/configuracion/usuarios', icon: 'manage_accounts' },
        ],
      },
      { label: 'Auditoría', path: '/auditoria', icon: 'policy' },
      { label: 'Configuración', path: '/configuracion', exact: true, icon: 'settings' },
    ];

  constructor() {
    void firstValueFrom(this.clinic.clinicProfile())
      .then((profile) => clinicName.set(profile.name))
      .catch(() => undefined);
  }

  logout() {
    void firstValueFrom(this.auth.logout()).finally(() => {
      clinicaSession.clear();
      location.href = '/ingreso';
    });
  }
}
