import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'ingreso', loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/shell/shell').then((m) => m.ShellPage),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      { path: 'inicio', loadComponent: () => import('./modules/shell/home').then((m) => m.HomePage) },
      { path: 'consultorios', canActivate: [roleGuard('SuperAdmin')], loadComponent: () => import('./modules/tenants/tenants').then((m) => m.TenantsPage) },
      {
        path: 'organizacion',
        canActivate: [roleGuard('TenantAdmin')],
        loadComponent: () => import('./modules/organization/organization').then((m) => m.OrganizationPage),
      },
      { path: 'empleados', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./modules/employees/employees').then((m) => m.EmployeesPage) },
      { path: 'agendas', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./modules/schedules/schedules').then((m) => m.SchedulesPage) },
      {
        path: 'historias',
        canActivate: [roleGuard('TenantAdmin')],
        loadComponent: () => import('./modules/medical-records/medical-records').then((m) => m.MedicalRecordsPage),
      },
      {
        path: 'historias/:patientId',
        canActivate: [roleGuard('TenantAdmin')],
        loadComponent: () => import('./modules/medical-records/medical-record-detail').then((m) => m.MedicalRecordDetailPage),
      },
      { path: 'aranceles', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./modules/fees/fees').then((m) => m.FeesPage) },
      { path: 'comprobantes', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./modules/invoices/invoices').then((m) => m.InvoicesPage) },
      { path: 'auditoria', canActivate: [roleGuard('TenantAdmin')], loadComponent: () => import('./modules/audit/audit-log').then((m) => m.AuditLogPage) },
    ],
  },
];
