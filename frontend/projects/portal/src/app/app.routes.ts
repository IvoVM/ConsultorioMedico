import { Routes } from '@angular/router';
import { TenantRole } from 'sdk';
import { authGuard, roleGuard, signedInGuard } from './auth.guard';
import { AppRoute, routePath } from './app.routes.enum';

export const routes: Routes = [
  { path: routePath(AppRoute.Ingreso), loadComponent: () => import('./modules/auth/welcome').then((m) => m.WelcomePage) },
  { path: routePath(AppRoute.IngresoEmpleado), loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage), data: { audience: 'staff' } },
  { path: routePath(AppRoute.IngresoCuenta), loadComponent: () => import('./modules/auth/login').then((m) => m.LoginPage), data: { audience: 'patient' } },
  { path: routePath(AppRoute.Registro), loadComponent: () => import('./modules/auth/register').then((m) => m.RegisterPage) },
  { path: routePath(AppRoute.Clave), canActivate: [signedInGuard], loadComponent: () => import('./modules/auth/change-password').then((m) => m.ChangePasswordPage) },
  { path: routePath(AppRoute.Paciente), redirectTo: routePath(AppRoute.Reservar) },
  {
    path: '',
    loadComponent: () => import('./modules/shell/shell').then((m) => m.ShellPage),
    children: [
      { path: routePath(AppRoute.Reservar), loadComponent: () => import('./modules/appointments/request-appointment').then((m) => m.RequestAppointmentPage) },
      {
        path: '',
        canActivate: [authGuard],
        children: [
          { path: '', pathMatch: 'full', redirectTo: routePath(AppRoute.Panel) },
          { path: routePath(AppRoute.Panel), loadComponent: () => import('./modules/shell/dashboard').then((m) => m.DashboardPage) },
          {
            path: routePath(AppRoute.MisTurnos),
            canActivate: [roleGuard(TenantRole.Patient)],
            loadComponent: () => import('./modules/appointments/my-appointments').then((m) => m.MyAppointmentsPage),
          },
          {
            path: routePath(AppRoute.Historia),
            canActivate: [roleGuard(TenantRole.Patient, TenantRole.Doctor, TenantRole.Secretary, TenantRole.TenantAdmin)],
            loadComponent: () => import('./modules/clinical/clinical-history').then((m) => m.ClinicalHistoryPage),
          },
          {
            path: routePath(AppRoute.Recetas),
            canActivate: [roleGuard(TenantRole.Patient)],
            loadComponent: () => import('./modules/prescriptions/prescriptions').then((m) => m.PrescriptionsPage),
          },
          {
            path: routePath(AppRoute.DarTurno),
            canActivate: [roleGuard(TenantRole.Secretary, TenantRole.TenantAdmin)],
            loadComponent: () => import('./modules/appointments/desk-booking').then((m) => m.DeskBookingPage),
          },
          {
            path: routePath(AppRoute.Solicitudes),
            canActivate: [roleGuard(TenantRole.Secretary, TenantRole.TenantAdmin)],
            loadComponent: () => import('./modules/appointments/desk-requests').then((m) => m.DeskRequestsPage),
          },
          {
            path: routePath(AppRoute.ReservarPaciente),
            canActivate: [roleGuard(TenantRole.Secretary, TenantRole.TenantAdmin)],
            loadComponent: () => import('./modules/appointments/book-appointment').then((m) => m.BookAppointmentPage),
          },
          {
            path: routePath(AppRoute.Secretaria),
            canActivate: [roleGuard(TenantRole.Secretary, TenantRole.TenantAdmin)],
            loadComponent: () => import('./modules/front-desk/front-desk').then((m) => m.FrontDeskPage),
          },
          { path: routePath(AppRoute.Medico), canActivate: [roleGuard(TenantRole.Doctor)], loadComponent: () => import('./modules/clinical/doctor').then((m) => m.DoctorPage) },
        ],
      },
    ],
  },
];
