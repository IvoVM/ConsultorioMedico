import { useRouter } from 'expo-router';
import { useState } from 'react';
import { register } from '../src/api';
import { Button, ErrorText, Field, Screen } from '../src/ui';

export default function Registro() {
  const router = useRouter();
  const [form, setForm] = useState({ email: '', password: '', firstName: '', lastName: '', documentNumber: '', birthDate: '', phone: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const set = (key: keyof typeof form) => (value: string) => setForm((current) => ({ ...current, [key]: value }));

  async function submit() {
    setBusy(true);
    setError('');
    try {
      await register({ ...form, email: form.email.trim() });
      router.replace('/(tabs)/panel');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo crear la cuenta.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen title="Cuenta de paciente" subtitle="La fecha de nacimiento va como AAAA-MM-DD.">
      <Field label="Nombre" value={form.firstName} onChangeText={set('firstName')} />
      <Field label="Apellido" value={form.lastName} onChangeText={set('lastName')} />
      <Field label="Email" autoCapitalize="none" keyboardType="email-address" value={form.email} onChangeText={set('email')} />
      <Field label="Contraseña" secureTextEntry value={form.password} onChangeText={set('password')} />
      <Field label="Documento" value={form.documentNumber} onChangeText={set('documentNumber')} />
      <Field label="Nacimiento" placeholder="1990-04-12" value={form.birthDate} onChangeText={set('birthDate')} />
      <Field label="Teléfono" keyboardType="phone-pad" value={form.phone} onChangeText={set('phone')} />
      <ErrorText text={error} />
      <Button label={busy ? 'Creando…' : 'Crear cuenta'} disabled={busy} onPress={() => void submit()} />
    </Screen>
  );
}
