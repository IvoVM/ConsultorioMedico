import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { clinicaSession } from 'sdk';

export const signedInGuard: CanActivateFn = () =>
  clinicaSession.token() ? true : inject(Router).createUrlTree(['/ingreso']);

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  if (!clinicaSession.token()) return router.createUrlTree(['/ingreso']);
  if (clinicaSession.mustChangePassword()) return router.createUrlTree(['/clave']);
  return true;
};

export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const router = inject(Router);
    if (clinicaSession.mustChangePassword()) return router.createUrlTree(['/clave']);
    const role = clinicaSession.role();
    return role && roles.includes(role) ? true : router.createUrlTree(['/ingreso']);
  };
}
