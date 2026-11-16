import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, hora, todayIso, type Appointment, type Diagnosis } from '../../src/api';
import { AppointmentStatus } from '../../src/status';
import { colors, type } from '../../src/theme';
import { Button, Card, Choice, Empty, ErrorText, Field, Screen } from '../../src/ui';

const closedStatuses: string[] = [AppointmentStatus.Cancelled, AppointmentStatus.NoShow, AppointmentStatus.Completed];

// The visit is a draft until it is signed with the professional's password; then it is a folio of the
// patient's record and can only be corrected. Prescriptions are not issued from the system for now.
export default function Medico() {
  const [items, setItems] = useState<Appointment[]>([]);
  const [picked, setPicked] = useState<Appointment | null>(null);
  const [encounterId, setEncounterId] = useState('');
  const [note, setNote] = useState('');
  const [plan, setPlan] = useState('');
  const [pressure, setPressure] = useState('');
  const [search, setSearch] = useState('');
  const [matches, setMatches] = useState<Diagnosis[]>([]);
  const [chosen, setChosen] = useState<Diagnosis[]>([]);
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  useEffect(() => {
    api.day(todayIso())
      .then(setItems)
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo abrir la agenda.'));
  }, []);

  async function open(item: Appointment) {
    setPicked(item);
    setError('');
    setNotice('');
    setPassword('');
    try {
      const draft = await api.encounterFor(item.id);
      setEncounterId(draft?.id ?? '');
      setNote(draft?.note ?? '');
      setPlan(draft?.plan ?? '');
      setPressure(draft?.bloodPressure ?? '');
      setChosen(draft?.diagnoses ?? []);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo abrir la consulta.');
    }
  }

  async function find(text: string) {
    setSearch(text);
    if (text.trim().length < 2) {
      setMatches([]);
      return;
    }
    try {
      const found = await api.diagnoses(text.trim());
      setMatches(found.filter((item) => !chosen.some((c) => c.id === item.id)));
    } catch {
      setMatches([]);
    }
  }

  async function save() {
    if (!picked) return '';
    setError('');
    try {
      const encounter = await api.saveEncounter({
        appointmentId: picked.id,
        note,
        plan,
        bloodPressure: pressure || null,
        diagnosisIds: chosen.map((item) => item.id),
      });
      setEncounterId(encounter.id);
      setNotice('Borrador guardado. Falta firmarlo.');
      return encounter.id;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo guardar la consulta.');
      return '';
    }
  }

  async function sign() {
    if (!password) {
      setError('Ingresá tu contraseña para firmar.');
      return;
    }
    const id = await save();
    if (!id) return;
    try {
      const entry = await api.signEncounter(id, password);
      setPicked(null);
      setEncounterId('');
      setPassword('');
      setNotice(`Consulta firmada como folio ${entry.folio}.`);
      setItems(await api.day(todayIso()));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo firmar.');
    }
  }

  return (
    <Screen title="Atención" subtitle="Elegí el turno, escribí la consulta y firmala.">
      <ErrorText text={error} />
      {notice ? <Text style={{ fontFamily: type.sans, color: colors.ink }}>{notice}</Text> : null}
      {items.length === 0 ? <Empty text="No tenés turnos hoy." /> : null}
      {items
        .filter((item) => !closedStatuses.includes(item.status))
        .map((item) => (
          <Card key={item.id} onPress={() => void open(item)}>
            <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{hora(item.start)} {item.patient}</Text>
            <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.appointmentType}</Text>
          </Card>
        ))}
      {picked ? (
        <>
          <Text style={{ fontFamily: type.serif, fontSize: 24, color: colors.deep }}>{picked.patient}</Text>
          <Field label="Evolución" multiline value={note} onChangeText={setNote} />
          <Field label="Indicaciones y tratamiento" multiline value={plan} onChangeText={setPlan} />
          <Field label="Tensión arterial" value={pressure} onChangeText={setPressure} />
          <Field label="Diagnóstico (CIE-10)" placeholder="Código o nombre" value={search} onChangeText={(text) => void find(text)} />
          {matches.map((item) => (
            <Choice
              key={item.id}
              label={`${item.code} ${item.name}`}
              selected={false}
              onPress={() => {
                setChosen((list) => [...list, item]);
                setMatches((list) => list.filter((m) => m.id !== item.id));
              }}
            />
          ))}
          {chosen.map((item) => (
            <Choice key={item.id} label={`${item.code} ${item.name}`} selected onPress={() => setChosen((list) => list.filter((c) => c.id !== item.id))} />
          ))}
          <Button label="Guardar borrador" tone="ghost" onPress={() => void save()} />
          <Field label="Tu contraseña para firmar" secureTextEntry value={password} onChangeText={setPassword} />
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>Al firmar pasa a la historia clínica y ya no se puede editar: los errores se corrigen con una corrección firmada.</Text>
          <Button label="Firmar y cerrar" onPress={() => void sign()} />
        </>
      ) : null}
    </Screen>
  );
}
