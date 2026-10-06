import { Component, inject, signal } from '@angular/core';
import { ClinicaClient, CreatedEmployeeDto, RejectedRowDto, errorMessage, roleLabels } from 'sdk';
import { UiButton, UiEmpty, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

@Component({
  imports: [UiButton, UiTable, UiEmpty],
  templateUrl: './employees.html',
})
export class EmployeesPage {
  private readonly api = inject(ClinicaClient);
  readonly roleLabels = roleLabels;
  readonly csv = signal('');
  readonly created = signal<CreatedEmployeeDto[]>([]);
  readonly rejected = signal<RejectedRowDto[]>([]);
  readonly error = signal('');

  readFile(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    void file.text().then((text) => this.csv.set(text));
  }

  async import() {
    this.error.set('');
    try {
      const result = await firstValueFrom(this.api.importEmployees({ csv: this.csv() }));
      this.created.set(result.created);
      this.rejected.set(result.rejected);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }
}
