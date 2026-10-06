import { HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { signal } from '@angular/core';
import { CLINICA_API_URL } from './tokens';

const TOKEN = 'clinica.token';
const TENANT = 'clinica.tenant';
const ROLE = 'clinica.role';
const NAME = 'clinica.name';

function read(key: string) {
  return globalThis.localStorage?.getItem(key) ?? null;
}

export const clinicaSession = {
  token: signal<string | null>(read(TOKEN)),
  tenant: signal<string | null>(read(TENANT)),
  role: signal<string | null>(read(ROLE)),
  name: signal<string | null>(read(NAME)),
  set(token: string, tenant: string | null, role: string, name: string) {
    localStorage.setItem(TOKEN, token);
    localStorage.setItem(ROLE, role);
    localStorage.setItem(NAME, name);
    this.token.set(token);
    this.role.set(role);
    this.name.set(name);
    this.setTenant(tenant);
  },
  setTenant(tenant: string | null) {
    if (tenant) localStorage.setItem(TENANT, tenant);
    else localStorage.removeItem(TENANT);
    this.tenant.set(tenant);
  },
  clear() {
    for (const key of [TOKEN, TENANT, ROLE, NAME]) localStorage.removeItem(key);
    this.token.set(null);
    this.tenant.set(null);
    this.role.set(null);
    this.name.set(null);
  },
};

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = clinicaSession.token();
  return token ? next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })) : next(req);
};

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenant = clinicaSession.tenant();
  if (!tenant || req.url.includes('/api/plataforma')) return next(req);
  return next(req.clone({ setHeaders: { 'X-Tenant-Slug': tenant } }));
};

export function provideClinicaSdk(apiUrl: string) {
  return [{ provide: CLINICA_API_URL, useValue: apiUrl }, provideHttpClient(withInterceptors([authInterceptor, tenantInterceptor]))];
}

export function tenantFromHost() {
  const host = globalThis.location?.hostname ?? '';
  return host.endsWith('.localhost') ? host.split('.')[0] : null;
}

export function errorMessage(error: unknown) {
  const http = error as { error?: { error?: string } };
  return http.error?.error ?? 'No se pudo completar la operación.';
}
