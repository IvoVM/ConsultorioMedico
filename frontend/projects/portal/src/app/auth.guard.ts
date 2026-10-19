import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { clinicaSession } from 'sdk';
import { AppRoute } from './app.routes.enum';

export const signedInGuard: CanActivateFn = () =>
  clinicaSession.token() ? true : inject(Router).createUrlTree([AppRoute.Ingreso]);

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  if (!clinicaSession.token()) return router.createUrlTree([AppRoute.Ingreso]);
  if (clinicaSession.mustChangePassword()) return router.createUrlTree([AppRoute.Clave]);
  return true;
};

export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const router = inject(Router);
    if (clinicaSession.mustChangePassword()) return router.createUrlTree([AppRoute.Clave]);
    const role = clinicaSession.role();
    return role && roles.includes(role) ? true : router.createUrlTree([AppRoute.Ingreso]);
  };
}
