import { Component, inject, signal } from '@angular/core';
import { CreatedEmployeeDto, EmployeesService, RejectedRowDto, errorMessage } from 'sdk';
import { roleLabels } from './models/role-labels';
import { UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiButton, UiTable, UiEmpty],
  templateUrl: './employees.html',
})
export class EmployeesPage {
  private readonly employees = inject(EmployeesService);
  readonly roleLabels = roleLabels;
  readonly csv = signal('');
  readonly created = signal<CreatedEmployeeDto[]>([]);
  readonly rejected = signal<RejectedRowDto[]>([]);
  readonly error = signal('');
  readonly saving = signal(false);

  readFile(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    void file.text().then((text) => this.csv.set(text));
  }

  async import() {
    this.error.set('');
    this.saving.set(true);
    try {
      const result = await firstValueFrom(this.employees.importEmployees({ csv: this.csv() }));
      this.created.set(result.created);
      this.rejected.set(result.rejected);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
