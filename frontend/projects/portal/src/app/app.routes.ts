import { Routes } from '@angular/router';
import { authGuard, roleGuard, signedInGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'ingreso', loadComponent: () => import('./modules/auth/welcome').then((m) => m.WelcomePage) },
  { path: 'ingreso/empleado', loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage), data: { audience: 'staff' } },
  { path: 'clave', canActivate: [signedInGuard], loadComponent: () => import('./modules/auth/change-password').then((m) => m.ChangePasswordPage) },
  { path: 'paciente', redirectTo: 'reservar' },
  {
    path: '',
    loadComponent: () => import('./modules/shell/shell').then((m) => m.ShellPage),
    children: [
      { path: 'reservar', loadComponent: () => import('./modules/appointments/request-appointment').then((m) => m.RequestAppointmentPage) },
      { path: 'registro', loadComponent: () => import('./modules/auth/register').then((m) => m.RegisterPage) },
      { path: 'ingreso/cuenta', loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage), data: { audience: 'patient' } },
      {
        path: '',
        canActivate: [authGuard],
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'panel' },
          { path: 'panel', loadComponent: () => import('./modules/shell/dashboard').then((m) => m.DashboardPage) },
          {
            path: 'mis-turnos',
            canActivate: [roleGuard('Patient')],
            loadComponent: () => import('./modules/appointments/my-appointments').then((m) => m.MyAppointmentsPage),
          },
          {
            path: 'historia',
            canActivate: [roleGuard('Patient', 'Doctor', 'Secretary', 'TenantAdmin')],
            loadComponent: () => import('./modules/clinical/clinical-history').then((m) => m.ClinicalHistoryPage),
          },
          {
            path: 'recetas',
            canActivate: [roleGuard('Patient')],
            loadComponent: () => import('./modules/prescriptions/prescriptions').then((m) => m.PrescriptionsPage),
          },
          {
            path: 'reservar/paciente',
            canActivate: [roleGuard('Secretary', 'TenantAdmin')],
            loadComponent: () => import('./modules/appointments/book-appointment').then((m) => m.BookAppointmentPage),
          },
          {
            path: 'secretaria',
            canActivate: [roleGuard('Secretary', 'TenantAdmin')],
            loadComponent: () => import('./modules/front-desk/front-desk').then((m) => m.FrontDeskPage),
          },
          { path: 'medico', canActivate: [roleGuard('Doctor')], loadComponent: () => import('./modules/clinical/doctor').then((m) => m.DoctorPage) },
        ],
      },
    ],
  },
];
