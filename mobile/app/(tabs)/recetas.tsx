import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, fechaCorta, type Prescription } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Card, Empty, ErrorText, Screen } from '../../src/ui';

export default function Recetas() {
  const [items, setItems] = useState<Prescription[]>([]);
  const [error, setError] = useState('');
  useEffect(() => {
    api.myPrescriptions().then(setItems).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer las recetas.'));
  }, []);
  return (
    <Screen title="Recetas">
      <ErrorText text={error} />
      {items.length === 0 ? <Empty text="No hay recetas en tu cuenta." /> : null}
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{fechaCorta(item.createdAt)} · {item.professional}</Text>
          {item.items.map((line) => (
            <Text key={line.medication} style={{ fontFamily: type.sans, color: colors.ink }}>{line.medication} {line.dose}, {line.frequency}, {line.duration}</Text>
          ))}
          {item.instructions ? <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.instructions}</Text> : null}
        </Card>
      ))}
    </Screen>
  );
}
