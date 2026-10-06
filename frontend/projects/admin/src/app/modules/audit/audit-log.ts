import { Component, inject, signal } from '@angular/core';
import { AuditEntryDto, AuditService, errorMessage } from 'sdk';
import { UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable],
  templateUrl: './audit-log.html',
})
export class AuditLogPage {
  private readonly audit = inject(AuditService);
  readonly entries = signal<AuditEntryDto[]>([]);
  readonly error = signal('');

  constructor() {
    void firstValueFrom(this.audit.auditLog())
      .then((list) => this.entries.set(list))
      .catch((error) => this.error.set(errorMessage(error)));
  }
}
