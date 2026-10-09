import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, fechaCorta, hora, statusLabel, type Appointment } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, Empty, ErrorText, Screen } from '../../src/ui';

export default function Turnos() {
  const [items, setItems] = useState<Appointment[]>([]);
  const [error, setError] = useState('');

  function load() {
    api.mine()
      .then((list) => setItems(list.sort((a, b) => b.start.localeCompare(a.start))))
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer los turnos.'));
  }

  useEffect(load, []);

  async function cancel(id: string) {
    setError('');
    try {
      await api.cancel(id);
      load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo cancelar.');
    }
  }

  return (
    <Screen title="Mis turnos">
      <ErrorText text={error} />
      {items.length === 0 ? <Empty text="Todavía no tenés turnos." /> : null}
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{fechaCorta(item.start)} {hora(item.start)}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.professional} · {item.appointmentType}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{statusLabel[item.status] ?? item.status}</Text>
          {item.status === 'Booked' ? <Button label="Cancelar" tone="pulse" onPress={() => void cancel(item.id)} /> : null}
        </Card>
      ))}
    </Screen>
  );
}
