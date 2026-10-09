import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, type MedicalRecord } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, ErrorText, Field, Screen } from '../../src/ui';

export default function Historias() {
  const [items, setItems] = useState<MedicalRecord[]>([]);
  const [current, setCurrent] = useState<MedicalRecord | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api.records().then(setItems).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer las historias.'));
  }, []);

  return (
    <Screen title="Historias clínicas">
      <ErrorText text={error} />
      {items.map((item) => (
        <Card key={item.patientId} onPress={() => setCurrent(item)}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{item.lastName}, {item.firstName}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.documentNumber}</Text>
        </Card>
      ))}
      {current ? (
        <>
          <Field label="Alergias" value={current.allergies ?? ''} onChangeText={(allergies) => setCurrent({ ...current, allergies })} />
          <Field label="Medicación" value={current.currentMedication ?? ''} onChangeText={(currentMedication) => setCurrent({ ...current, currentMedication })} />
          <Field label="Obra social" value={current.healthInsurance ?? ''} onChangeText={(healthInsurance) => setCurrent({ ...current, healthInsurance })} />
          <Field label="Notas" multiline value={current.notes ?? ''} onChangeText={(notes) => setCurrent({ ...current, notes })} />
          <Button
            label="Guardar historia"
            onPress={() => {
              const { patientId, email: _email, ...body } = current;
              api.saveRecord(patientId, body)
                .then((saved) => {
                  setCurrent(saved);
                  setItems((list) => list.map((item) => (item.patientId === saved.patientId ? saved : item)));
                })
                .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo guardar.'));
            }}
          />
        </>
      ) : null}
    </Screen>
  );
}
