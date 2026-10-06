import { HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { signal } from '@angular/core';
import { CLINICA_API_URL } from './tokens';

const TOKEN = 'clinica.token';
const ROLE = 'clinica.role';
const NAME = 'clinica.name';

function read(key: string) {
  return globalThis.localStorage?.getItem(key) ?? null;
}

export const clinicaSession = {
  token: signal<string | null>(read(TOKEN)),
  role: signal<string | null>(read(ROLE)),
  name: signal<string | null>(read(NAME)),
  set(token: string, role: string, name: string) {
    localStorage.setItem(TOKEN, token);
    localStorage.setItem(ROLE, role);
    localStorage.setItem(NAME, name);
    localStorage.removeItem('clinica.tenant');
    this.token.set(token);
    this.role.set(role);
    this.name.set(name);
  },
  clear() {
    for (const key of [TOKEN, ROLE, NAME, 'clinica.tenant']) localStorage.removeItem(key);
    this.token.set(null);
    this.role.set(null);
    this.name.set(null);
  },
};

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = clinicaSession.token();
  return token ? next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })) : next(req);
};

export function provideClinicaSdk(apiUrl: string) {
  return [{ provide: CLINICA_API_URL, useValue: apiUrl }, provideHttpClient(withInterceptors([authInterceptor]))];
}

export function errorMessage(error: unknown) {
  const http = error as { error?: { error?: string } };
  return http.error?.error ?? 'No se pudo completar la operación.';
}
