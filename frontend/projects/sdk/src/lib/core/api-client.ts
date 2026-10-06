import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { CLINICA_API_URL } from './tokens';

export abstract class ApiClient {
  protected readonly http = inject(HttpClient);
  protected readonly base = inject(CLINICA_API_URL);
}
