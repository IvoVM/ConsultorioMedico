import { Routes } from '@angular/router';
import { TenantRole } from 'sdk';
import { authGuard, roleGuard, signedInGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'ingreso', loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage) },
  { path: 'clave', canActivate: [signedInGuard], loadComponent: () => import('./modules/auth/change-password').then((m) => m.ChangePasswordPage) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/shell/shell').then((m) => m.ShellPage),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      { path: 'inicio', loadComponent: () => import('./modules/shell/home').then((m) => m.HomePage) },
      {
        path: 'organizacion',
        canActivate: [roleGuard(TenantRole.TenantAdmin)],
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'servicios' },
          {
            path: 'especialidades',
            children: [
              { path: '', loadComponent: () => import('./modules/organization/specialties').then((m) => m.SpecialtiesPage) },
              { path: ':specialtyId', loadComponent: () => import('./modules/organization/specialty-detail').then((m) => m.SpecialtyDetailPage) },
            ],
          },
          { path: 'obras-sociales', loadComponent: () => import('./modules/organization/insurers').then((m) => m.InsurersPage) },
          { path: 'tipos-de-turno', redirectTo: 'especialidades' },
          { path: 'turnos', redirectTo: 'especialidades' },
          { path: 'turnos/:turnoId', redirectTo: 'especialidades' },
          {
            path: ':section',
            loadComponent: () => import('./modules/organization/organization').then((m) => m.OrganizationPage),
          },
        ],
      },
      { path: 'empleados', canActivate: [roleGuard(TenantRole.TenantAdmin)], loadComponent: () => import('./modules/employees/employees').then((m) => m.EmployeesPage) },
      { path: 'pacientes', canActivate: [roleGuard(TenantRole.TenantAdmin)], loadComponent: () => import('./modules/patients/patients').then((m) => m.PatientsPage) },
      {
        path: 'pacientes/:patientId',
        canActivate: [roleGuard(TenantRole.TenantAdmin)],
        loadComponent: () => import('./modules/patients/patient-file').then((m) => m.PatientFilePage),
      },
      { path: 'agendas', canActivate: [roleGuard(TenantRole.TenantAdmin)], loadComponent: () => import('./modules/schedules/schedules').then((m) => m.SchedulesPage) },
      {
        path: 'historias',
        canActivate: [roleGuard(TenantRole.TenantAdmin)],
        loadComponent: () => import('./modules/medical-records/medical-records').then((m) => m.MedicalRecordsPage),
      },
      {
        path: 'historias/:patientId',
        canActivate: [roleGuard(TenantRole.TenantAdmin)],
        loadComponent: () => import('./modules/medical-records/medical-record-detail').then((m) => m.MedicalRecordDetailPage),
      },
      { path: 'aranceles', redirectTo: 'organizacion/especialidades' },
      { path: 'comprobantes', canActivate: [roleGuard(TenantRole.TenantAdmin)], loadComponent: () => import('./modules/invoices/invoices').then((m) => m.InvoicesPage) },
      { path: 'auditoria', canActivate: [roleGuard(TenantRole.TenantAdmin)], loadComponent: () => import('./modules/audit/audit-log').then((m) => m.AuditLogPage) },
      { path: 'configuracion/usuarios', canActivate: [roleGuard(TenantRole.TenantAdmin)], loadComponent: () => import('./modules/users/users').then((m) => m.UsersPage) },
    ],
  },
];
