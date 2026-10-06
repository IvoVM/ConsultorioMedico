import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService, clinicaSession } from 'sdk';
import { firstValueFrom } from 'rxjs';
import { NavNode, UiButton, UiIcon, UiNav, UiPage } from 'ui';

@Component({
  imports: [RouterOutlet, RouterLink, UiPage, UiButton, UiIcon, UiNav],
  templateUrl: './shell.html',
})
export class ShellPage {
  private readonly auth = inject(AuthService);
  readonly session = clinicaSession;
  readonly menu = computed((): NavNode[] => {
    const role = this.session.role();
    const nodes: NavNode[] = [];
    if (this.session.token()) nodes.push({ label: 'Inicio', path: '/panel', exact: true });
    if (role === 'Patient') {
      nodes.push({
        label: 'Turnos',
        children: [
          { label: 'Reservar', path: '/reservar' },
          { label: 'Mis turnos', path: '/mis-turnos' },
        ],
      });
      nodes.push({
        label: 'Mi salud',
        children: [
          { label: 'Historia', path: '/historia' },
          { label: 'Recetas', path: '/recetas' },
        ],
      });
    } else {
      nodes.push({ label: 'Reservar', path: '/reservar' });
    }
    if (role === 'Doctor') {
      nodes.push({
        label: 'Consultorio',
        children: [
          { label: 'Atención', path: '/medico' },
          { label: 'Historia', path: '/historia' },
        ],
      });
    }
    if (role === 'Secretary' || role === 'TenantAdmin') nodes.push({ label: 'Secretaría', path: '/secretaria' });
    return nodes;
  });

  logout() {
    void firstValueFrom(this.auth.logout()).finally(() => {
      clinicaSession.clear();
      location.href = '/ingreso';
    });
  }
}
