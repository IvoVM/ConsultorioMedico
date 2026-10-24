import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService, ClinicService, TenantRole, clinicaSession } from 'sdk';
import { firstValueFrom } from 'rxjs';
import { NavNode, UiButton, UiIcon, UiNav, UiPage } from 'ui';
import { readClinicName } from '../auth/clinic-name';
import { AppRoute } from '../../app.routes.enum';

@Component({
  imports: [RouterOutlet, RouterLink, UiPage, UiButton, UiIcon, UiNav],
  templateUrl: './shell.html',
})
export class ShellPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  readonly session = clinicaSession;
  readonly route = AppRoute;
  readonly clinicName = signal('Consultorio');
  readonly menu = computed((): NavNode[] => {
    const role = this.session.role();
    const nodes: NavNode[] = [];
    if (this.session.token()) nodes.push({ label: 'Inicio', path: AppRoute.Panel, exact: true });
    if (role === TenantRole.Patient) {
      nodes.push({
        label: 'Turnos',
        children: [
          { label: 'Reservar', path: AppRoute.Reservar },
          { label: 'Mis turnos', path: AppRoute.MisTurnos },
        ],
      });
      nodes.push({
        label: 'Mi salud',
        children: [
          { label: 'Historia', path: AppRoute.Historia },
          { label: 'Perfil', path: AppRoute.Perfil },
          { label: 'Recetas', path: AppRoute.Recetas },
        ],
      });
    } else if (role === TenantRole.Secretary || role === TenantRole.TenantAdmin) {
      nodes.push({
        label: 'Turnos',
        children: [
          { label: 'Dar un turno', path: AppRoute.DarTurno },
          { label: 'Solicitudes', path: AppRoute.Solicitudes },
        ],
      });
    } else if (this.session.token()) {
      nodes.push({ label: 'Reservar', path: AppRoute.Reservar });
    }
    if (role === TenantRole.Doctor) {
      nodes.push({
        label: 'Consultorio',
        children: [
          { label: 'Atención', path: AppRoute.Medico },
          { label: 'Historia', path: AppRoute.Historia },
        ],
      });
    }
    if (role === TenantRole.Secretary || role === TenantRole.TenantAdmin) nodes.push({ label: 'Secretaría', path: AppRoute.Secretaria });
    return nodes;
  });

  constructor() {
    if (this.session.token() && this.session.mustChangePassword()) void this.router.navigateByUrl(AppRoute.Clave);
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
  }

  logout() {
    void firstValueFrom(this.auth.logout()).finally(() => {
      clinicaSession.clear();
      location.href = AppRoute.Ingreso;
    });
  }
}
