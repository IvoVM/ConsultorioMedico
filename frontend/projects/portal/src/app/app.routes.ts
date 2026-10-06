import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'ingreso', loadComponent: () => import('./modules/auth/welcome').then((m) => m.WelcomePage) },
  { path: 'ingreso/empleado', loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage), data: { audience: 'staff' } },
  { path: 'ingreso/cuenta', loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage), data: { audience: 'patient' } },
  { path: 'paciente', loadComponent: () => import('./modules/auth/patient-entry').then((m) => m.PatientEntryPage) },
  { path: 'registro', loadComponent: () => import('./modules/auth/register').then((m) => m.RegisterPage) },
  {
    path: '',
    loadComponent: () => import('./modules/shell/shell').then((m) => m.ShellPage),
    children: [
      { path: 'reservar', loadComponent: () => import('./modules/appointments/book-appointment').then((m) => m.BookAppointmentPage) },
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
            canActivate: [roleGuard('Patient', 'Doctor', 'Secretary')],
            loadComponent: () => import('./modules/clinical/clinical-history').then((m) => m.ClinicalHistoryPage),
          },
          {
            path: 'recetas',
            canActivate: [roleGuard('Patient')],
            loadComponent: () => import('./modules/prescriptions/prescriptions').then((m) => m.PrescriptionsPage),
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
