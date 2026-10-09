import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, type AppointmentType, type Location, type Professional, type ScheduleBlock } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, Choice, ErrorText, Field, Screen } from '../../src/ui';

const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const dayLabel: Record<string, string> = { Monday: 'Lunes', Tuesday: 'Martes', Wednesday: 'Miércoles', Thursday: 'Jueves', Friday: 'Viernes', Saturday: 'Sábado' };

export default function Agendas() {
  const [professionals, setProfessionals] = useState<Professional[]>([]);
  const [locations, setLocations] = useState<Location[]>([]);
  const [types, setTypes] = useState<AppointmentType[]>([]);
  const [professionalId, setProfessionalId] = useState('');
  const [blocks, setBlocks] = useState<ScheduleBlock[]>([]);
  const [day, setDay] = useState('Monday');
  const [start, setStart] = useState('09:00:00');
  const [end, setEnd] = useState('13:00:00');
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([api.professionals(), api.locations(), api.types()])
      .then(([people, places, kinds]) => {
        setProfessionals(people);
        setLocations(places);
        setTypes(kinds);
        if (people[0]) setProfessionalId(people[0].id);
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer las agendas.'));
  }, []);

  useEffect(() => {
    if (!professionalId) return;
    api.schedule(professionalId).then(setBlocks).catch(() => setBlocks([]));
  }, [professionalId]);

  function add() {
    if (!locations[0] || !types[0]) return;
    setBlocks((current) => [
      ...current,
      { id: '00000000-0000-0000-0000-000000000000', day, startTime: start, endTime: end, locationId: locations[0].id, appointmentTypeId: types[0].id },
    ]);
  }

  return (
    <Screen title="Agendas" subtitle="Los bloques se guardan para el profesional elegido.">
      <ErrorText text={error} />
      {professionals.map((item) => (
        <Choice key={item.id} label={`${item.firstName} ${item.lastName}`} selected={item.id === professionalId} onPress={() => setProfessionalId(item.id)} />
      ))}
      {blocks.map((item, index) => (
        <Card key={`${item.day}-${item.startTime}-${index}`}>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{dayLabel[item.day] ?? item.day} {item.startTime.slice(0, 5)}–{item.endTime.slice(0, 5)}</Text>
        </Card>
      ))}
      <ViewDays day={day} setDay={setDay} />
      <Field label="Desde" value={start} onChangeText={setStart} />
      <Field label="Hasta" value={end} onChangeText={setEnd} />
      <Button label="Sumar bloque" tone="ghost" onPress={add} />
      <Button
        label="Guardar agenda"
        onPress={() => {
          api.saveSchedule(professionalId, blocks).then(setBlocks).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo guardar.'));
        }}
      />
    </Screen>
  );
}

function ViewDays({ day, setDay }: { day: string; setDay: (value: string) => void }) {
  return (
    <>
      {days.map((item) => (
        <Choice key={item} label={dayLabel[item]} selected={item === day} onPress={() => setDay(item)} />
      ))}
    </>
  );
}
