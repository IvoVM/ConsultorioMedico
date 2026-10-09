import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, type AppointmentType, type Fee } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, Choice, ErrorText, Field, Screen } from '../../src/ui';

export default function Aranceles() {
  const [fees, setFees] = useState<Fee[]>([]);
  const [types, setTypes] = useState<AppointmentType[]>([]);
  const [typeId, setTypeId] = useState('');
  const [amount, setAmount] = useState('');
  const [from, setFrom] = useState(new Date().toISOString().slice(0, 10));
  const [error, setError] = useState('');

  function load() {
    Promise.all([api.fees(), api.types()])
      .then(([nextFees, nextTypes]) => {
        setFees(nextFees);
        setTypes(nextTypes);
        setTypeId((current) => current || nextTypes[0]?.id || '');
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudieron leer los aranceles.'));
  }

  useEffect(load, []);

  return (
    <Screen title="Aranceles">
      <ErrorText text={error} />
      {fees.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{item.appointmentType}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>${item.amount} desde {item.effectiveFrom}</Text>
        </Card>
      ))}
      {types.map((item) => (
        <Choice key={item.id} label={item.name} selected={item.id === typeId} onPress={() => setTypeId(item.id)} />
      ))}
      <Field label="Monto" keyboardType="decimal-pad" value={amount} onChangeText={setAmount} />
      <Field label="Vigente desde" value={from} onChangeText={setFrom} />
      <Button
        label="Crear arancel"
        onPress={() => {
          api.createFee(typeId, Number(amount), from).then(() => load()).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo crear.'));
        }}
      />
    </Screen>
  );
}
