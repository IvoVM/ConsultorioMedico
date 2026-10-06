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
        label: 'Organización',
        children: [
          { label: 'Servicios', path: '/organizacion/servicios' },
          { label: 'Especialidades', path: '/organizacion/especialidades' },
          { label: 'Tipos de turno', path: '/organizacion/tipos-de-turno' },
        ],
      },
      {
        label: 'Personas',
        children: [
          { label: 'Empleados', path: '/empleados' },
          { label: 'Pacientes', path: '/pacientes' },
          { label: 'Historias médicas', path: '/historias' },
        ],
      },
      { label: 'Agendas', path: '/agendas' },
      {
        label: 'Cobros',
        children: [
          { label: 'Aranceles', path: '/aranceles' },
          { label: 'Comprobantes', path: '/comprobantes' },
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
