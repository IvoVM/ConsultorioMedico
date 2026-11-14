import { Component, computed, effect, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UiBadge } from './ui-badge.component';
import { UiButton } from './ui-button.component';
import { UiField } from './ui-field.component';
import { UiIcon } from './ui-icon.component';
import { UiModal } from './ui-modal.component';
import { UiSelect, SelectOption } from './ui-select.component';
import { formatFecha } from './fecha';

// The invoice as each app reads it: labels already resolved from its own enums, so this library needs no SDK.
export type InvoiceView = {
  heading: string;
  status: string;
  tone: 'ok' | 'warn' | 'muted';
  patient: string;
  patientDocument: string;
  professional: string;
  appointmentStart: string;
  items: { description: string; quantity: number; unitPrice: number; amount: number }[];
  subtotal: number;
  discount: number;
  total: number;
  insurer: string | null;
  insurerAmount: number;
  patientAmount: number;
  paid: number;
  balance: number;
  payments: { id: string; amount: number; method: string; paidAt: string }[];
  cae: string | null;
  caeDueDate: string | null;
  fiscalSimulated: boolean;
  voidReason: string | null;
  creditNote: string | null;
  editable: boolean;
  canPay: boolean;
  canAuthorize: boolean;
  canVoid: boolean;
};

export type InvoiceEdit = {
  items: { description: string; quantity: number; unitPrice: number }[];
  discount: number;
  insurerAmount: number;
};

export type InvoiceSource = {
  save(edit: InvoiceEdit): Promise<void>;
  pay(amount: number, method: string): Promise<void>;
  authorize(): Promise<void>;
  void(reason: string): Promise<void>;
  pdf(): Promise<void>;
};

export enum InvoiceMode {
  View = 'view',
  Edit = 'edit',
  Void = 'void',
}

const money = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', minimumFractionDigits: 2 });

export function formatMoney(amount: number | null | undefined) {
  return money.format(amount ?? 0);
}

function failure(error: unknown, fallback: string) {
  return error instanceof Error && error.message ? error.message : fallback;
}

@Component({
  selector: 'ui-invoice',
  imports: [FormsModule, UiBadge, UiButton, UiField, UiIcon, UiModal, UiSelect],
  templateUrl: './ui-invoice.component.html',
})
export class UiInvoice {
  readonly view = input.required<InvoiceView>();
  readonly source = input.required<InvoiceSource>();
  // Payment methods as value/label pairs from the app's enum.
  readonly methods = input<SelectOption[]>([]);
  readonly closed = output<void>();

  readonly modes = InvoiceMode;
  readonly mode = signal<InvoiceMode>(InvoiceMode.View);
  readonly busy = signal('');
  readonly error = signal('');
  readonly lines = signal<{ description: string; quantity: number; unitPrice: number }[]>([]);
  readonly discount = signal(0);
  readonly insurerAmount = signal(0);
  readonly amount = signal(0);
  readonly method = signal('');
  readonly reason = signal('');
  readonly draftSubtotal = computed(() => this.lines().reduce((sum, line) => sum + (Number(line.quantity) || 0) * (Number(line.unitPrice) || 0), 0));
  readonly draftTotal = computed(() => this.draftSubtotal() - (Number(this.discount()) || 0));

  readonly money = formatMoney;

  constructor() {
    // A fresh invoice (after a payment, say) resets the amount to what is still owed.
    effect(() => {
      const view = this.view();
      untracked(() => {
        this.amount.set(view.balance);
        if (!this.method()) this.method.set(this.methods()[0]?.value ?? '');
      });
    });
  }

  date(iso: string | null) {
    return formatFecha(iso);
  }

  startEdit() {
    const view = this.view();
    this.lines.set(view.items.map((item) => ({ description: item.description, quantity: item.quantity, unitPrice: item.unitPrice })));
    this.discount.set(view.discount);
    this.insurerAmount.set(view.insurerAmount);
    this.error.set('');
    this.mode.set(InvoiceMode.Edit);
  }

  addLine() {
    this.lines.update((lines) => [...lines, { description: '', quantity: 1, unitPrice: 0 }]);
  }

  removeLine(index: number) {
    this.lines.update((lines) => lines.filter((_, i) => i !== index));
  }

  setLine(index: number, field: 'description' | 'quantity' | 'unitPrice', value: string | number) {
    this.lines.update((lines) =>
      lines.map((line, i) => (i === index ? { ...line, [field]: field === 'description' ? String(value) : Number(value) } : line)),
    );
  }

  startVoid() {
    this.reason.set('');
    this.error.set('');
    this.mode.set(InvoiceMode.Void);
  }

  back() {
    this.error.set('');
    this.mode.set(InvoiceMode.View);
  }

  async save() {
    await this.run('save', 'No se pudo guardar el comprobante.', async () => {
      await this.source().save({
        items: this.lines().map((line) => ({ description: line.description.trim(), quantity: Number(line.quantity), unitPrice: Number(line.unitPrice) })),
        discount: Number(this.discount()) || 0,
        insurerAmount: Number(this.insurerAmount()) || 0,
      });
      this.mode.set(InvoiceMode.View);
    });
  }

  async pay() {
    await this.run('pay', 'No se pudo registrar el pago.', () => this.source().pay(Number(this.amount()), this.method()));
  }

  async authorize() {
    await this.run('authorize', 'No se pudo autorizar el comprobante.', () => this.source().authorize());
  }

  async confirmVoid() {
    await this.run('void', 'No se pudo anular el comprobante.', async () => {
      await this.source().void(this.reason().trim());
      this.mode.set(InvoiceMode.View);
    });
  }

  async pdf() {
    await this.run('pdf', 'No se pudo descargar el PDF.', () => this.source().pdf());
  }

  private async run(key: string, fallback: string, action: () => Promise<void>) {
    this.busy.set(key);
    this.error.set('');
    try {
      await action();
    } catch (error) {
      this.error.set(failure(error, fallback));
    } finally {
      this.busy.set('');
    }
  }
}
