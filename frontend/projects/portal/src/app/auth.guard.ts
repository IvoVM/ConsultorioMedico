import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { clinicaSession } from 'sdk';

export const authGuard: CanActivateFn = () =>
  clinicaSession.token() ? true : inject(Router).createUrlTree(['/login']);

export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const rol = clinicaSession.rol();
    return rol && roles.includes(rol) ? true : inject(Router).createUrlTree(['/login']);
  };
}
