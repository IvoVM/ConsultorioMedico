import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'ingreso', loadComponent: () => import('./pages/login').then((m) => m.LoginPage) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/shell').then((m) => m.ShellPage),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      { path: 'inicio', loadComponent: () => import('./pages/home').then((m) => m.HomePage) },
      { path: 'consultorios', canActivate: [roleGuard('SuperAdmin')], loadComponent: () => import('./pages/tenants').then((m) => m.TenantsPage) },
      { path: 'organizacion', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/organization').then((m) => m.OrganizationPage) },
      { path: 'empleados', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/employees').then((m) => m.EmployeesPage) },
      { path: 'agendas', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/schedules').then((m) => m.SchedulesPage) },
      { path: 'historias', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/medical-records').then((m) => m.MedicalRecordsPage) },
      { path: 'historias/:patientId', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/medical-record-detail').then((m) => m.MedicalRecordDetailPage) },
      { path: 'aranceles', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/fees').then((m) => m.FeesPage) },
      { path: 'comprobantes', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/invoices').then((m) => m.InvoicesPage) },
      { path: 'auditoria', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./pages/audit-log').then((m) => m.AuditLogPage) },
    ],
  },
];
