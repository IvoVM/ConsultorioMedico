import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService, clinicaSession } from 'sdk';
import { firstValueFrom } from 'rxjs';
import { NavNode, UiButton, UiIcon, UiNav, UiPage } from 'ui';

@Component({
  imports: [RouterOutlet, UiPage, UiButton, UiIcon, UiNav],
  templateUrl: './shell.html',
})
export class ShellPage {
  private readonly auth = inject(AuthService);
  readonly session = clinicaSession;
  readonly menu = computed((): NavNode[] => [
      { label: 'Inicio', path: '/inicio', exact: true },
      {
        label: 'Agenda',
        children: [
          { label: 'Solicitudes de turnos', path: '/turnos/solicitudes' },
          { label: 'Historial de turnos', path: '/turnos/historial' },
          { label: 'Agendas', path: '/agendas' },
        ],
      },
      {
        label: 'Pacientes',
        children: [
          { label: 'Pacientes', path: '/pacientes' },
          { label: 'Historias médicas', path: '/historias' },
        ],
      },
      {
        label: 'Atención',
        children: [
          { label: 'Especialidades', path: '/organizacion/especialidades' },
          { label: 'Servicios', path: '/organizacion/servicios' },
        ],
      },
      {
        label: 'Facturación',
        children: [
          { label: 'Obras sociales', path: '/organizacion/obras-sociales' },
          { label: 'Comprobantes', path: '/comprobantes' },
        ],
      },
      {
        label: 'Personal',
        children: [
          { label: 'Empleados', path: '/empleados' },
          { label: 'Usuarios', path: '/configuracion/usuarios' },
        ],
      },
      { label: 'Auditoría', path: '/auditoria' },
    ]);

  logout() {
    void firstValueFrom(this.auth.logout()).finally(() => {
      clinicaSession.clear();
      location.href = '/ingreso';
    });
  }
}
