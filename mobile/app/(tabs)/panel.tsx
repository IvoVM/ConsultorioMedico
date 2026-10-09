import { useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, diaLargo, fechaCorta, hora, statusLabel, todayIso, type Appointment } from '../../src/api';
import { logout } from '../../src/api';
import { useSession } from '../../src/session';
import { colors, type } from '../../src/theme';
import { Button, Card, Empty, ErrorText, Screen } from '../../src/ui';

export default function Panel() {
  const { current } = useSession();
  const router = useRouter();
  const [items, setItems] = useState<Appointment[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!current) return;
    const load = current.role === 'Patient' ? api.mine() : api.day(todayIso());
    load
      .then((list) => {
        const upcoming = current.role === 'Patient'
          ? list.filter((item) => item.status !== 'Cancelled' && item.status !== 'NoShow' && new Date(item.start) >= new Date(new Date().setHours(0, 0, 0, 0)))
          : list;
        setItems(upcoming.sort((a, b) => a.start.localeCompare(b.start)));
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo cargar el día.'));
  }, [current]);

  return (
    <Screen title={current?.name ?? 'Hoy'} subtitle={diaLargo()}>
      <ErrorText text={error} />
      {items.length === 0 ? <Empty text="No hay turnos para mostrar." /> : null}
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{hora(item.start)} · {statusLabel[item.status] ?? item.status}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{current?.role === 'Patient' ? item.professional : item.patient}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.appointmentType} · {fechaCorta(item.start)}</Text>
        </Card>
      ))}
      <Button
        label="Salir"
        tone="ghost"
        onPress={() => {
          void logout().then(() => router.replace('/'));
        }}
      />
    </Screen>
  );
}
