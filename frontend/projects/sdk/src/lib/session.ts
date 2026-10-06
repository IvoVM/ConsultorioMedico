import { HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { signal } from '@angular/core';
import { CLINICA_API_URL } from './tokens';

const TOKEN = 'clinica.token';
const TENANT = 'clinica.tenant';
const ROL = 'clinica.rol';
const NOMBRE = 'clinica.nombre';

function leer(clave: string) {
  return globalThis.localStorage?.getItem(clave) ?? null;
}

export const clinicaSession = {
  token: signal<string | null>(leer(TOKEN)),
  tenant: signal<string | null>(leer(TENANT)),
  rol: signal<string | null>(leer(ROL)),
  nombre: signal<string | null>(leer(NOMBRE)),
  set(token: string, tenant: string | null, rol: string, nombre: string) {
    localStorage.setItem(TOKEN, token);
    localStorage.setItem(ROL, rol);
    localStorage.setItem(NOMBRE, nombre);
    this.token.set(token);
    this.rol.set(rol);
    this.nombre.set(nombre);
    this.setTenant(tenant);
  },
  setTenant(tenant: string | null) {
    if (tenant) localStorage.setItem(TENANT, tenant);
    else localStorage.removeItem(TENANT);
    this.tenant.set(tenant);
  },
  clear() {
    for (const clave of [TOKEN, TENANT, ROL, NOMBRE]) localStorage.removeItem(clave);
    this.token.set(null);
    this.tenant.set(null);
    this.rol.set(null);
    this.nombre.set(null);
  },
};

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = clinicaSession.token();
  return token ? next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })) : next(req);
};

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenant = clinicaSession.tenant();
  if (!tenant || req.url.includes('/api/platform')) return next(req);
  return next(req.clone({ setHeaders: { 'X-Tenant-Slug': tenant } }));
};

export function provideClinicaSdk(apiUrl: string) {
  return [{ provide: CLINICA_API_URL, useValue: apiUrl }, provideHttpClient(withInterceptors([authInterceptor, tenantInterceptor]))];
}

export function tenantDesdeHost() {
  const host = globalThis.location?.hostname ?? '';
  return host.endsWith('.localhost') ? host.split('.')[0] : null;
}

export function mensajeError(error: unknown) {
  const http = error as { error?: { error?: string } };
  return http.error?.error ?? 'No se pudo completar la operación.';
}
