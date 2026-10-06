import { Component, inject, signal } from '@angular/core';
import { AuditEntryDto, AuditService, errorMessage } from 'sdk';
import { UiSkeleton, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiTable, UiSkeleton],
  templateUrl: './audit-log.html',
})
export class AuditLogPage {
  private readonly audit = inject(AuditService);
  readonly entries = signal<AuditEntryDto[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);

  constructor() {
    void firstValueFrom(this.audit.auditLog())
      .then((list) => this.entries.set(list))
      .catch((error) => this.error.set(errorMessage(error)))
      .finally(() => this.loading.set(false));
  }
}
