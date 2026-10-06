import { Component, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { clinicaSession } from 'sdk';
import { NavNode, UiButton, UiNav, UiPage } from 'ui';

@Component({
  imports: [RouterOutlet, UiPage, UiButton, UiNav],
  templateUrl: './shell.html',
})
export class ShellPage {
  readonly session = clinicaSession;
  readonly menu = computed((): NavNode[] => [
      { label: 'Inicio', path: '/inicio', exact: true },
      {
        label: 'Organización',
        children: [
          { label: 'Sedes', path: '/organizacion/sedes' },
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
    clinicaSession.clear();
    location.href = '/ingreso';
  }
}
