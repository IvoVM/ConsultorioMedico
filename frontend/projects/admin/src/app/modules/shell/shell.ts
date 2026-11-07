import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService, clinicaSession } from 'sdk';
import { firstValueFrom } from 'rxjs';
import { NavNode, UiNav, UiPage } from 'ui';
import { roleLabels } from '../employees/models/role-labels';

@Component({
  imports: [RouterOutlet, UiPage, UiNav],
  templateUrl: './shell.html',
})
export class ShellPage {
  private readonly auth = inject(AuthService);
  readonly session = clinicaSession;
  readonly roleLabel = computed(() => {
    const role = this.session.role();
    return role ? roleLabels[role] : 'Administración';
  });
  readonly menu = computed((): NavNode[] => [
      { label: 'Inicio', path: '/inicio', exact: true, icon: 'monitor_heart' },
      {
        label: 'Agenda',
        icon: 'calendar_month',
        children: [
          { label: 'Solicitudes de turnos', path: '/turnos/solicitudes', icon: 'pending_actions' },
          { label: 'Historial de turnos', path: '/turnos/historial', icon: 'history' },
          { label: 'Agendas', path: '/agendas', icon: 'event_note' },
        ],
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
    ]);

  logout() {
    void firstValueFrom(this.auth.logout()).finally(() => {
      clinicaSession.clear();
      location.href = '/ingreso';
    });
  }
}
