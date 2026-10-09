import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, fechaCorta, hora, type AuditEntry } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Card, ErrorText, Screen } from '../../src/ui';

export default function Auditoria() {
  const [items, setItems] = useState<AuditEntry[]>([]);
  const [error, setError] = useState('');
  useEffect(() => {
    api.audit().then(setItems).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo leer la auditoría.'));
  }, []);
  return (
    <Screen title="Auditoría">
      <ErrorText text={error} />
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{item.action} · {item.entity}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{fechaCorta(item.timestamp)} {hora(item.timestamp)}{item.detail ? ` · ${item.detail}` : ''}</Text>
        </Card>
      ))}
    </Screen>
  );
}
