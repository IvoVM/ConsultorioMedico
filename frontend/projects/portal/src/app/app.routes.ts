import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login').then((m) => m.LoginPage) },
  { path: 'registro', loadComponent: () => import('./pages/registro').then((m) => m.RegistroPage) },
  {
    path: '',
    loadComponent: () => import('./pages/shell').then((m) => m.ShellPage),
    children: [
      { path: 'reservar', loadComponent: () => import('./pages/reservar').then((m) => m.ReservarPage) },
      {
        path: '',
        canActivate: [authGuard],
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'panel' },
          { path: 'panel', loadComponent: () => import('./pages/panel').then((m) => m.PanelPage) },
          { path: 'mis-turnos', canActivate: [roleGuard('Paciente')], loadComponent: () => import('./pages/mis-turnos').then((m) => m.MisTurnosPage) },
          { path: 'historia', canActivate: [roleGuard('Paciente', 'Medico', 'Secretario')], loadComponent: () => import('./pages/historia').then((m) => m.HistoriaPage) },
          { path: 'recetas', canActivate: [roleGuard('Paciente')], loadComponent: () => import('./pages/recetas').then((m) => m.RecetasPage) },
          { path: 'secretaria', canActivate: [roleGuard('Secretario', 'AdminTenant')], loadComponent: () => import('./pages/secretaria').then((m) => m.SecretariaPage) },
          { path: 'medico', canActivate: [roleGuard('Medico')], loadComponent: () => import('./pages/medico').then((m) => m.MedicoPage) },
        ],
      },
    ],
  },
];
