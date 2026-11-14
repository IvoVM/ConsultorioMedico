import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService, ClinicService, TenantRole, clinicaSession } from 'sdk';
import { firstValueFrom } from 'rxjs';
import { NavNode, UiNav, UiPage } from 'ui';
import { readClinicName } from '../auth/clinic-name';
import { AppRoute } from '../../app.routes.enum';
import { roleLabel } from './models/role-labels';

@Component({
  imports: [RouterOutlet, RouterLink, UiPage, UiNav],
  templateUrl: './shell.html',
})
export class ShellPage {
  private readonly auth = inject(AuthService);
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  readonly session = clinicaSession;
  readonly route = AppRoute;
  readonly roleText = computed(() => roleLabel(this.session.role()));
  readonly clinicName = signal('Consultorio');
  readonly menu = computed((): NavNode[] => {
    const role = this.session.role();
    const nodes: NavNode[] = [];
    if (this.session.token()) nodes.push({ label: 'Inicio', path: AppRoute.Panel, exact: true, icon: 'monitor_heart' });
    if (role === TenantRole.Secretary || role === TenantRole.TenantAdmin || role === TenantRole.Doctor) nodes.push({ label: 'Agenda', path: AppRoute.Agenda, icon: 'calendar_view_week' });
    if (role === TenantRole.Patient) {
      // Booking is the patient's main action, so it stands alone; what they already booked lives under Turnos.
      nodes.push({ label: 'Reservar turno', path: AppRoute.Reservar, icon: 'event_upcoming' });
      nodes.push({
        label: 'Turnos',
        icon: 'calendar_month',
        children: [
          { label: 'Historial de turnos', path: AppRoute.MisTurnos, icon: 'event_available' },
          { label: 'Cancelados', path: AppRoute.TurnosCancelados, icon: 'event_busy' },
        ],
      });
      nodes.push({
        label: 'Mi salud',
        icon: 'favorite',
        children: [
          { label: 'Historia', path: AppRoute.Historia, icon: 'clinical_notes' },
          { label: 'Recetas', path: AppRoute.Recetas, icon: 'prescriptions' },
        ],
      });
      nodes.push({ label: 'Mis comprobantes', path: AppRoute.MisComprobantes, icon: 'receipt_long' });
      // The profile holds the patient's own details, not their health, so it gets its own entry.
      nodes.push({ label: 'Mi perfil', path: AppRoute.Perfil, icon: 'person' });
    } else if (role === TenantRole.Secretary || role === TenantRole.TenantAdmin) {
      nodes.push({
        label: 'Turnos',
        icon: 'calendar_month',
        children: [
          { label: 'Dar un turno', path: AppRoute.DarTurno, icon: 'add_circle' },
          { label: 'Solicitudes', path: AppRoute.Solicitudes, icon: 'pending_actions' },
          { label: 'Historial', path: AppRoute.HistorialTurnos, icon: 'history' },
        ],
      });
    } else if (this.session.token()) {
      nodes.push({ label: 'Reservar', path: AppRoute.Reservar, icon: 'event_upcoming' });
    }
    if (role === TenantRole.Doctor) {
      nodes.push({
        label: 'Consultorio',
        icon: 'medical_services',
        children: [
          { label: 'Atención', path: AppRoute.Medico, icon: 'stethoscope' },
          { label: 'Historia', path: AppRoute.Historia, icon: 'clinical_notes' },
          { label: 'Turnos', path: AppRoute.HistorialTurnos, icon: 'history' },
        ],
      });
    }
    if (role === TenantRole.Secretary || role === TenantRole.TenantAdmin) nodes.push({ label: 'Secretaría', path: AppRoute.Secretaria, icon: 'desk' });
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
