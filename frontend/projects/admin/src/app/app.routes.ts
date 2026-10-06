import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login').then((m) => m.LoginPage) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/shell').then((m) => m.ShellPage),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      { path: 'inicio', loadComponent: () => import('./pages/inicio').then((m) => m.InicioPage) },
      { path: 'tenants', canActivate: [roleGuard('SuperAdmin')], loadComponent: () => import('./pages/tenants').then((m) => m.TenantsPage) },
      { path: 'organizacion', canActivate: [roleGuard('AdminTenant')], loadComponent: () => import('./pages/organizacion').then((m) => m.OrganizacionPage) },
      { path: 'empleados', canActivate: [roleGuard('AdminTenant')], loadComponent: () => import('./pages/empleados').then((m) => m.EmpleadosPage) },
      { path: 'agendas', canActivate: [roleGuard('AdminTenant')], loadComponent: () => import('./pages/agendas').then((m) => m.AgendasPage) },
      { path: 'aranceles', canActivate: [roleGuard('AdminTenant')], loadComponent: () => import('./pages/aranceles').then((m) => m.ArancelesPage) },
      { path: 'comprobantes', canActivate: [roleGuard('AdminTenant')], loadComponent: () => import('./pages/comprobantes').then((m) => m.ComprobantesPage) },
      { path: 'auditoria', canActivate: [roleGuard('AdminTenant')], loadComponent: () => import('./pages/auditoria').then((m) => m.AuditoriaPage) },
    ],
  },
];
