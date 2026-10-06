import { HttpBackend, HttpClient, HttpErrorResponse, HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { inject, provideAppInitializer, signal } from '@angular/core';
import { Observable, catchError, finalize, firstValueFrom, map, shareReplay, switchMap, throwError } from 'rxjs';
import type { TokenDto } from '../modules/auth/models/token-dto';
import { CLINICA_API_URL } from './tokens';

const LEGACY_KEYS = ['clinica.token', 'clinica.tenant', 'clinica.role', 'clinica.name'];

export const clinicaSession = {
  token: signal<string | null>(null),
  role: signal<string | null>(null),
  name: signal<string | null>(null),
  set(token: string, role: string, name: string) {
    for (const key of LEGACY_KEYS) localStorage.removeItem(key);
    this.token.set(token);
    this.role.set(role);
    this.name.set(name);
  },
  clear() {
    for (const key of LEGACY_KEYS) localStorage.removeItem(key);
    this.token.set(null);
    this.role.set(null);
    this.name.set(null);
  },
};

let refresh$: Observable<string> | null = null;

function isRefresh(url: string) {
  return url.includes('/api/acceso/renovar');
}

function isCredentialCall(url: string) {
  return url.includes('/api/acceso/');
}

function refreshRequest(api: string, backend: HttpBackend) {
  return new HttpClient(backend).post<TokenDto>(`${api}/api/acceso/renovar`, {}, { withCredentials: true });
}

function refreshAccess(api: string, backend: HttpBackend) {
  if (!refresh$) {
    refresh$ = refreshRequest(api, backend).pipe(
      map((dto) => {
        clinicaSession.set(dto.token, dto.role, dto.name);
        return dto.token;
      }),
      catchError((error: unknown) => {
        clinicaSession.clear();
        return throwError(() => error);
      }),
      finalize(() => {
        refresh$ = null;
      }),
      shareReplay(1),
    );
  }
  return refresh$;
}

function logoutOnce() {
  clinicaSession.clear();
  const path = globalThis.location?.pathname ?? '';
  if (path.startsWith('/ingreso') || path.startsWith('/registro')) return;
  globalThis.location.href = '/ingreso';
}

export const credentialsInterceptor: HttpInterceptorFn = (req, next) =>
  isCredentialCall(req.url) ? next(req.clone({ withCredentials: true })) : next(req);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const api = inject(CLINICA_API_URL);
  const backend = inject(HttpBackend);
  const token = clinicaSession.token();
  const authed = token && !isCredentialCall(req.url) ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authed).pipe(
    catchError((error: unknown) => {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      if (status !== 401 || isRefresh(req.url) || !authed.headers.has('Authorization')) return throwError(() => error);
      return refreshAccess(api, backend).pipe(
        switchMap((nextToken) => next(req.clone({ setHeaders: { Authorization: `Bearer ${nextToken}` } }))),
        catchError((refreshError: unknown) => {
          logoutOnce();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};

function restoreSession() {
  const backend = inject(HttpBackend);
  const api = inject(CLINICA_API_URL);
  return firstValueFrom(refreshRequest(api, backend)).then(
    (dto) => clinicaSession.set(dto.token, dto.role, dto.name),
    () => clinicaSession.clear(),
  );
}

export function provideClinicaSdk(apiUrl: string) {
  return [
    { provide: CLINICA_API_URL, useValue: apiUrl },
    provideHttpClient(withInterceptors([credentialsInterceptor, authInterceptor])),
    provideAppInitializer(restoreSession),
  ];
}

export function errorMessage(error: unknown) {
  const http = error as { error?: { error?: string } };
  return http.error?.error ?? 'No se pudo completar la operación.';
}
