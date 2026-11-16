import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClinicalService, DiagnosisDto, ImportedDiagnosesDto, errorMessage } from 'sdk';
import { UiButton, UiEmpty, UiField, UiHelp, UiTable } from 'ui';
import { firstValueFrom } from 'rxjs';

// The CIE-10 catalog professionals pick diagnoses from (Ley 26.529, art. 15 asks for WHO nomenclature). The
// clinic loads the official file it is licensed to use; codes are added or renamed, never removed.
@Component({
  imports: [FormsModule, UiButton, UiEmpty, UiField, UiHelp, UiTable],
  templateUrl: './diagnoses.html',
})
export class DiagnosesPage {
  private readonly clinical = inject(ClinicalService);
  readonly results = signal<DiagnosisDto[]>([]);
  readonly imported = signal<ImportedDiagnosesDto | null>(null);
  readonly error = signal('');
  readonly importing = signal(false);
  readonly searched = signal(false);
  query = '';
  private timer?: ReturnType<typeof setTimeout>;

  constructor() {
    void this.find();
  }

  onSearch() {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => void this.find(), 250);
  }

  async find() {
    try {
      this.results.set(await firstValueFrom(this.clinical.diagnoses({ q: this.query.trim() })));
      this.searched.set(true);
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async load(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.importing.set(true);
    this.error.set('');
    this.imported.set(null);
    try {
      this.imported.set(await firstValueFrom(this.clinical.importDiagnoses({ csv: await file.text() })));
      await this.find();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.importing.set(false);
      input.value = '';
    }
  }
}
