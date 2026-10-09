import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, hora, statusLabel, todayIso, type Appointment, type WaitlistEntry } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, Empty, ErrorText, Screen } from '../../src/ui';

export default function Secretaria() {
  const [items, setItems] = useState<Appointment[]>([]);
  const [waiting, setWaiting] = useState<WaitlistEntry[]>([]);
  const [error, setError] = useState('');

  function load() {
    Promise.all([api.day(todayIso()), api.waitlist()])
      .then(([day, list]) => {
        setItems(day);
        setWaiting(list);
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo abrir el mostrador.'));
  }

  useEffect(load, []);

  async function run(task: () => Promise<unknown>) {
    setError('');
    try {
      await task();
      load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo actualizar.');
    }
  }

  return (
    <Screen title="Mostrador" subtitle="Turnos de hoy y lista de espera.">
      <ErrorText text={error} />
      {items.length === 0 ? <Empty text="No hay turnos hoy." /> : null}
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{hora(item.start)} {item.patient}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.professional} · {statusLabel[item.status] ?? item.status}</Text>
          {item.status === 'Booked' ? <Button label="Admitir" onPress={() => void run(() => api.checkIn(item.id))} /> : null}
          {item.status === 'Booked' || item.status === 'CheckedIn' ? <Button label="Cancelar" tone="ghost" onPress={() => void run(() => api.cancel(item.id))} /> : null}
        </Card>
      ))}
      {waiting.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{item.patient}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{statusLabel[item.status] ?? item.status}{item.notes ? ` · ${item.notes}` : ''}</Text>
        </Card>
      ))}
    </Screen>
  );
}
