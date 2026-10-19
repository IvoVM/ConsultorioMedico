import { useState } from 'react';
import { Text } from 'react-native';
import { formatPhone } from '../../src/account';
import { api, type PatientCard } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, ErrorText, Field, Screen } from '../../src/ui';

export default function Pacientes() {
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<PatientCard[]>([]);
  const [form, setForm] = useState({ email: '', firstName: '', lastName: '', documentNumber: '', birthDate: '', phone: '' });
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const set = (key: keyof typeof form) => (value: string) => setForm((current) => ({ ...current, [key]: value }));

  return (
    <Screen title="Pacientes" subtitle="Buscá por nombre o documento, o dalos de alta.">
      <Field label="Buscar" value={query} onChangeText={setQuery} />
      <Button
        label="Buscar"
        onPress={() => {
          api.searchPatients(query.trim()).then(setItems).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Sin resultados.'));
        }}
      />
      {items.map((item) => (
        <Card key={item.patientId}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{item.lastName}, {item.firstName}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.documentNumber} · {item.phone}</Text>
        </Card>
      ))}
      <Field label="Nombre" value={form.firstName} onChangeText={set('firstName')} />
      <Field label="Apellido" value={form.lastName} onChangeText={set('lastName')} />
      <Field label="Email" autoCapitalize="none" value={form.email} onChangeText={set('email')} />
      <Field label="Documento" value={form.documentNumber} onChangeText={set('documentNumber')} />
      <Field label="Nacimiento" placeholder="AAAA-MM-DD" value={form.birthDate} onChangeText={set('birthDate')} />
      <Field label="Teléfono" keyboardType="phone-pad" placeholder="+54 2291 542049" value={form.phone} onChangeText={set('phone')} onBlur={() => { const formatted = formatPhone(form.phone); if (formatted) set('phone')(formatted); }} />
      <ErrorText text={error} />
      {password ? <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>Clave temporal: {password}</Text> : null}
      <Button
        label="Crear paciente"
        onPress={() => {
          setError('');
          const phone = formatPhone(form.phone);
          if (!phone) {
            setError('El teléfono no es válido.');
            return;
          }
          api.createPatient({ ...form, phone })
            .then((created) => setPassword(created.temporaryPassword))
            .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo crear.'));
        }}
      />
    </Screen>
  );
}
