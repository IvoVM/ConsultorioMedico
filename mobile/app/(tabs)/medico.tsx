import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, hora, todayIso, type Appointment, type Diagnosis } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, Choice, Empty, ErrorText, Field, Screen } from '../../src/ui';

export default function Medico() {
  const [items, setItems] = useState<Appointment[]>([]);
  const [diagnoses, setDiagnoses] = useState<Diagnosis[]>([]);
  const [picked, setPicked] = useState<Appointment | null>(null);
  const [encounterId, setEncounterId] = useState('');
  const [note, setNote] = useState('');
  const [pressure, setPressure] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [medication, setMedication] = useState('');
  const [dose, setDose] = useState('');
  const [frequency, setFrequency] = useState('');
  const [duration, setDuration] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([api.day(todayIso()), api.diagnoses()])
      .then(([day, list]) => {
        setItems(day);
        setDiagnoses(list);
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo abrir la agenda.'));
  }, []);

  function toggle(id: string) {
    setSelected((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  async function save() {
    if (!picked) return '';
    setError('');
    try {
      const encounter = await api.saveEncounter({
        appointmentId: picked.id,
        note,
        bloodPressure: pressure || null,
        diagnosisIds: selected,
      });
      setEncounterId(encounter.id);
      return encounter.id;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo guardar el encuentro.');
      return '';
    }
  }

  async function close() {
    const id = encounterId || (await save());
    if (!id) return;
    try {
      await api.closeEncounter(id);
      setPicked(null);
      setEncounterId('');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo cerrar.');
    }
  }

  async function prescribe() {
    if (!encounterId) return;
    try {
      await api.prescribe({
        encounterId,
        items: [{ medication, dose, frequency, duration }],
      });
      setMedication('');
      setDose('');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo emitir la receta.');
    }
  }

  return (
    <Screen title="Atención" subtitle="Elegí el turno, cargá la nota y cerrá el encuentro.">
      <ErrorText text={error} />
      {items.length === 0 ? <Empty text="No tenés turnos hoy." /> : null}
      {items.map((item) => (
        <Card key={item.id} onPress={() => setPicked(item)}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{hora(item.start)} {item.patient}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.appointmentType}</Text>
        </Card>
      ))}
      {picked ? (
        <>
          <Text style={{ fontFamily: type.serif, fontSize: 24, color: colors.deep }}>{picked.patient}</Text>
          <Field label="Nota clínica" multiline value={note} onChangeText={setNote} />
          <Field label="Tensión" value={pressure} onChangeText={setPressure} />
          <ViewChoices diagnoses={diagnoses} selected={selected} toggle={toggle} />
          <Button label="Guardar encuentro" onPress={() => void save()} />
          <Button label="Cerrar encuentro" tone="ghost" onPress={() => void close()} />
          <Field label="Medicación" value={medication} onChangeText={setMedication} />
          <Field label="Dosis" value={dose} onChangeText={setDose} />
          <Field label="Frecuencia" value={frequency} onChangeText={setFrequency} />
          <Field label="Duración" value={duration} onChangeText={setDuration} />
          <Button label="Emitir receta" disabled={!encounterId} onPress={() => void prescribe()} />
        </>
      ) : null}
    </Screen>
  );
}

function ViewChoices({ diagnoses, selected, toggle }: { diagnoses: Diagnosis[]; selected: string[]; toggle: (id: string) => void }) {
  return (
    <>
      {diagnoses.map((item) => (
        <Choice key={item.id} label={`${item.code} ${item.name}`} selected={selected.includes(item.id)} onPress={() => toggle(item.id)} />
      ))}
    </>
  );
}
