import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { useRouter } from 'expo-router';
import { api, fechaCorta, hora, statusLabel, type Appointment, type BookingRequest } from '../../src/api';
import { AppointmentStatus, BookingRequestStatus } from '../../src/status';
import { colors, type } from '../../src/theme';
import { Button, Card, Empty, ErrorText, Screen } from '../../src/ui';

const requestLabel: Record<string, string> = {
  [BookingRequestStatus.Pending]: 'Pendiente de confirmación',
  [BookingRequestStatus.Approved]: 'Confirmada',
  [BookingRequestStatus.Rejected]: 'Denegada',
  [BookingRequestStatus.Rescheduled]: 'Reprogramada',
};

export default function Turnos() {
  const [items, setItems] = useState<Appointment[]>([]);
  const [requests, setRequests] = useState<BookingRequest[]>([]);
  const [error, setError] = useState('');
  const [reason, setReason] = useState<BookingRequest | null>(null);
  const router = useRouter();

  function load() {
    api.mine()
      .then((list) => setItems(list.sort((a, b) => b.start.localeCompare(a.start))))
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer los turnos.'));
    api.myRequests()
      .then((list) => setRequests(list.sort((a, b) => a.start.localeCompare(b.start))))
      .catch(() => setRequests([]));
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
    <Screen title="Mis turnos" subtitle="La secretaría confirma cada solicitud. Cuando lo hace, el turno queda asociado a tu documento: recordá llevarlo al consultorio.">
      <ErrorText text={error} />
      {requests.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{fechaCorta(item.start)} {hora(item.start)}</Text>
          {item.status === BookingRequestStatus.Rescheduled && item.previousStart ? (
            <Text style={{ fontFamily: type.sans, color: colors.ink }}>Antes: {fechaCorta(item.previousStart)} {hora(item.previousStart)}</Text>
          ) : null}
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.professional} · {item.specialty}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{requestLabel[item.status] ?? item.status}</Text>
          {item.status === BookingRequestStatus.Rejected ? (
            <Button label="Ver motivo" tone="ghost" onPress={() => setReason(item)} />
          ) : null}
        </Card>
      ))}
      {reason ? (
        <Card>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>No se confirmó el turno</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{reason.rejectionReason || 'La secretaría no dejó un motivo.'}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>Podés pedir otro turno.</Text>
          <Button label="Pedir otro turno" onPress={() => router.push('/(tabs)/reservar')} />
          <Button label="Cerrar" tone="ghost" onPress={() => setReason(null)} />
        </Card>
      ) : null}
      {items.length === 0 && requests.length === 0 ? <Empty text="Todavía no tenés turnos." /> : null}
      {items.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{fechaCorta(item.start)} {hora(item.start)}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.professional} · {item.appointmentType}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{statusLabel[item.status] ?? item.status}</Text>
          {item.status === AppointmentStatus.Booked || item.status === AppointmentStatus.Rescheduled ? <Button label="Cancelar" tone="pulse" onPress={() => void cancel(item.id)} /> : null}
        </Card>
      ))}
    </Screen>
  );
}
