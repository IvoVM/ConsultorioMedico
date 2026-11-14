import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AccountDto, AccountMovementDto, AccountMovementKind, BillingService, CardBrand, CashSessionDto, ClinicService, InvoiceDto, PaymentMethod, errorMessage } from 'sdk';
import { FechaPipe, SelectOption, ToastService, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiInvoice, UiModal, UiSelect, UiSkeleton, UiTable, formatMoney } from 'ui';
import { firstValueFrom } from 'rxjs';
import { AppRoute, RouteParam } from '../../app.routes.enum';
import { accountMovementKindLabels, cardBrandLabels, invoiceStatusLabels, paymentMethodLabels } from './models/invoice-labels';
import { cardBrandOptions, invoiceSource, invoiceTitle, invoiceTone, invoiceView, paymentOptions, voucherNumber } from './models/invoice-view';
import { AccountDialog } from './models/account-dialog';

// A patient's running account for front desk: what they owe across invoices, the credit they left,
// overdue installments, every movement with its running balance, and payments on account or refunds.
@Component({
  imports: [FormsModule, RouterLink, FechaPipe, UiBadge, UiButton, UiEmpty, UiField, UiHelp, UiInvoice, UiModal, UiSelect, UiSkeleton, UiTable],
  templateUrl: './account.html',
})
export class AccountPage {
  private readonly billing = inject(BillingService);
  private readonly clinic = inject(ClinicService);
  private readonly toasts = inject(ToastService);
  private readonly patientId = inject(ActivatedRoute).snapshot.queryParamMap.get(RouteParam.Paciente) ?? '';
  readonly route = AppRoute;
  readonly money = formatMoney;
  readonly dialogs = AccountDialog;
  readonly statusLabels = invoiceStatusLabels;
  readonly title = invoiceTitle;
  readonly tone = invoiceTone;
  readonly methodOptions = paymentOptions;
  readonly brandOptions = cardBrandOptions;
  readonly cardMethod = PaymentMethod.Card;
  readonly account = signal<AccountDto | null>(null);
  readonly registers = signal<CashSessionDto[]>([]);
  readonly fiscalEnabled = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly dialogError = signal('');
  readonly dialog = signal<AccountDialog | null>(null);
  readonly opened = signal<InvoiceDto | null>(null);
  readonly locationId = signal('');
  readonly amount = signal<number | null>(null);
  readonly method = signal<string>(PaymentMethod.Cash);
  readonly brand = signal<string>(CardBrand.Visa);
  readonly cardInstallments = signal(1);
  readonly batch = signal('');
  readonly coupon = signal('');
  readonly note = signal('');
  readonly registerOptions = computed((): SelectOption[] => this.registers().map((item) => ({ value: item.locationId, label: item.location })));
  readonly openedView = computed(() => {
    const invoice = this.opened();
    return invoice ? invoiceView(invoice, { staff: true, fiscalEnabled: this.fiscalEnabled() }) : null;
  });
  readonly openedSource = computed(() => {
    const invoice = this.opened();
    return invoice
      ? invoiceSource(this.billing, invoice, async (updated) => {
          this.opened.set(updated);
          await this.load();
        })
      : null;
  });

  constructor() {
    void this.load();
    void firstValueFrom(this.clinic.clinicProfile()).then((profile) => this.fiscalEnabled.set(profile.fiscalEnabled)).catch(() => undefined);
  }

  concept(item: AccountMovementDto) {
    const label = accountMovementKindLabels[item.kind];
    if (item.kind === AccountMovementKind.Invoice || item.kind === AccountMovementKind.Voided) {
      const number = voucherNumber(item.pointOfSale, item.number);
      return number ? `${label} ${number}` : `${label} sin autorizar`;
    }
    const method = item.cardBrand ? cardBrandLabels[item.cardBrand] : item.method ? paymentMethodLabels[item.method] : '';
    return method ? `${label} · ${method}` : label;
  }

  invoiceOf(item: AccountMovementDto) {
    return this.account()?.invoices.find((invoice) => invoice.id === item.invoiceId) ?? null;
  }

  async openDialog(dialog: AccountDialog) {
    this.dialogError.set('');
    this.amount.set(dialog === AccountDialog.Refund ? this.account()?.credit ?? null : this.account()?.owed || null);
    this.method.set(PaymentMethod.Cash);
    this.batch.set('');
    this.coupon.set('');
    this.note.set('');
    try {
      const open = await firstValueFrom(this.billing.openCashSessions());
      this.registers.set(open);
      this.locationId.set(open[0]?.locationId ?? '');
    } catch (error) {
      this.error.set(errorMessage(error));
      return;
    }
    this.dialog.set(dialog);
  }

  async pay() {
    const card = this.method() === PaymentMethod.Card;
    await this.save(async () => {
      this.account.set(
        await firstValueFrom(
          this.billing.accountPayment(this.patientId, {
            locationId: this.locationId(),
            amount: Number(this.amount()) || 0,
            method: this.method() as PaymentMethod,
            cardBrand: card ? (this.brand() as CardBrand) : null,
            cardInstallments: card ? Number(this.cardInstallments()) : null,
            cardBatch: card ? this.batch().trim() : null,
            cardCoupon: card ? this.coupon().trim() : null,
            note: this.note().trim() || null,
          }),
        ),
      );
      this.toasts.success('Pago registrado', 'Se aplicó a los comprobantes más viejos; lo que sobró queda a favor.');
    });
  }

  async refund() {
    await this.save(async () => {
      this.account.set(
        await firstValueFrom(
          this.billing.refund(this.patientId, {
            locationId: this.locationId(),
            amount: Number(this.amount()) || 0,
            method: this.method() as PaymentMethod,
            reason: this.note().trim(),
          }),
        ),
      );
      this.toasts.success('Devolución registrada', 'Salió de la caja de la sede.');
    });
  }

  private async save(action: () => Promise<void>) {
    this.saving.set(true);
    this.dialogError.set('');
    try {
      await action();
      this.dialog.set(null);
    } catch (error) {
      this.dialogError.set(errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  private async load() {
    if (!this.patientId) {
      this.error.set('Elegí un paciente desde la búsqueda de la secretaría.');
      this.loading.set(false);
      return;
    }
    try {
      this.account.set(await firstValueFrom(this.billing.account(this.patientId)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }
}
