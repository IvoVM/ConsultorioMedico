import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, fechaCorta, type Invoice } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, ErrorText, Screen } from '../../src/ui';

const methods = [
  ['Cash', 'Efectivo'],
  ['Transfer', 'Transferencia'],
  ['Card', 'Tarjeta'],
] as const;

export default function Comprobantes() {
  const [items, setItems] = useState<Invoice[]>([]);
  const [error, setError] = useState('');

  function load() {
    api.invoices().then(setItems).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer los comprobantes.'));
  }

  useEffect(load, []);

  return (
    <Screen title="Comprobantes">
      <ErrorText text={error} />
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{item.patient} · ${item.total}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.status} · {fechaCorta(item.createdAt)}</Text>
          {item.status === 'Pending'
            ? methods.map(([method, label]) => (
                <Button
                  key={method}
                  label={label}
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
