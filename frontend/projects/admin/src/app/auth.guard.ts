import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { clinicaSession } from 'sdk';

export const authGuard: CanActivateFn = () =>
  clinicaSession.token() ? true : inject(Router).createUrlTree(['/ingreso']);

export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const role = clinicaSession.role();
    return role && roles.includes(role) ? true : inject(Router).createUrlTree(['/ingreso']);
  };
}
