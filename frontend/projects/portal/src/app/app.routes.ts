import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'ingreso', loadComponent: () => import('./pages/login').then((m) => m.LoginPage) },
  { path: 'registro', loadComponent: () => import('./pages/register').then((m) => m.RegisterPage) },
  {
    path: '',
    loadComponent: () => import('./pages/shell').then((m) => m.ShellPage),
    children: [
      { path: 'reservar', loadComponent: () => import('./pages/book-appointment').then((m) => m.BookAppointmentPage) },
      {
        path: '',
        canActivate: [authGuard],
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'panel' },
          { path: 'panel', loadComponent: () => import('./pages/dashboard').then((m) => m.DashboardPage) },
          { path: 'mis-turnos', canActivate: [roleGuard('Patient')], loadComponent: () => import('./pages/my-appointments').then((m) => m.MyAppointmentsPage) },
          { path: 'historia', canActivate: [roleGuard('Patient', 'Doctor', 'Secretary')], loadComponent: () => import('./pages/clinical-history').then((m) => m.ClinicalHistoryPage) },
          { path: 'recetas', canActivate: [roleGuard('Patient')], loadComponent: () => import('./pages/prescriptions').then((m) => m.PrescriptionsPage) },
          { path: 'secretaria', canActivate: [roleGuard('Secretary', 'TenantAdmin')], loadComponent: () => import('./pages/front-desk').then((m) => m.FrontDeskPage) },
          { path: 'medico', canActivate: [roleGuard('Doctor')], loadComponent: () => import('./pages/doctor').then((m) => m.DoctorPage) },
        ],
      },
    ],
  },
];
