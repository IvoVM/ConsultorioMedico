import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, fechaCorta, type Invoice } from '../../src/api';
import { TenantRole } from '../../src/role';
import { useSession } from '../../src/session';
import { InvoiceStatus, PaymentMethod, invoiceStatusLabels, paymentMethodLabels } from '../../src/status';
import { colors, type } from '../../src/theme';
import { Button, Card, ErrorText, Screen } from '../../src/ui';

const money = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 2 });

function number(item: Invoice) {
  return item.number != null && item.pointOfSale != null
    ? `${String(item.pointOfSale).padStart(4, '0')}-${String(item.number).padStart(8, '0')}`
    : 'Sin autorizar';
}

// Staff charge the balance here; a patient reads their own invoices and what they still owe.
export default function Comprobantes() {
  const { current } = useSession();
  const patient = current?.role === TenantRole.Patient;
  const [items, setItems] = useState<Invoice[]>([]);
  const [error, setError] = useState('');

  function load() {
    (patient ? api.myInvoices() : api.invoices())
      .then(setItems)
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer los comprobantes.'));
  }

  useEffect(load, [patient]);

  return (
    <Screen title={patient ? 'Mis comprobantes' : 'Comprobantes'} subtitle={patient ? 'El PDF se descarga desde el portal web.' : undefined}>
      <ErrorText text={error} />
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>
            {patient ? item.professional : item.patient} · {money.format(item.total)}
          </Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>
            {invoiceStatusLabels[item.status]} · {fechaCorta(item.appointmentStart)} · {number(item)}
          </Text>
          {item.insurerAmount ? (
            <Text style={{ fontFamily: type.sans, color: colors.ink }}>
              Cubre {item.insurer}: {money.format(item.insurerAmount)} · a cargo del paciente {money.format(item.patientAmount)}
            </Text>
          ) : null}
          {item.payments.map((payment) => (
            <Text key={payment.id} style={{ fontFamily: type.sans, color: colors.ink }}>
              {fechaCorta(payment.paidAt)} · {paymentMethodLabels[payment.method]} · {money.format(payment.amount)}
            </Text>
          ))}
          {item.balance > 0 && item.status !== InvoiceStatus.Voided ? (
            <Text style={{ fontFamily: type.sansBold, color: colors.ink }}>Saldo {money.format(item.balance)}</Text>
          ) : null}
          {!patient && item.balance > 0 && item.status !== InvoiceStatus.Voided
            ? Object.values(PaymentMethod)
                // A card payment needs its batch and coupon, which are entered from the portal.
                .filter((method) => method !== PaymentMethod.Card)
                .map((method) => (
                <Button
                  key={method}
                  label={`Cobrar saldo · ${paymentMethodLabels[method]}`}
                  tone="ghost"
                  onPress={() => {
                    api.pay(item.id, method).then(load).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo cobrar.'));
                  }}
                />
              ))
            : null}
        </Card>
      ))}
    </Screen>
  );
}
