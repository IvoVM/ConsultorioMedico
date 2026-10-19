import { useRouter } from 'expo-router';
import { useState } from 'react';
import { formatPhone, passwordIssue } from '../src/account';
import { register } from '../src/api';
import { Button, ErrorText, Field, Screen } from '../src/ui';

export default function Registro() {
  const router = useRouter();
  const [form, setForm] = useState({ email: '', password: '', confirm: '', firstName: '', lastName: '', documentNumber: '', birthDate: '', phone: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const set = (key: keyof typeof form) => (value: string) => setForm((current) => ({ ...current, [key]: value }));

  async function submit() {
    setError('');
    const issue = passwordIssue(form.password);
    if (issue) {
      setError(issue);
      return;
    }
    if (form.password !== form.confirm) {
      setError('Las contraseñas no coinciden.');
      return;
    }
    const phone = formatPhone(form.phone);
    if (!phone) {
      setError('El teléfono no es válido.');
      return;
    }
    setBusy(true);
    try {
      const { confirm: _confirm, ...body } = form;
      await register({ ...body, email: form.email.trim(), phone });
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
      <Field label="Confirmar contraseña" secureTextEntry value={form.confirm} onChangeText={set('confirm')} />
      <Field label="Documento" value={form.documentNumber} onChangeText={set('documentNumber')} />
      <Field label="Nacimiento" placeholder="1990-04-12" value={form.birthDate} onChangeText={set('birthDate')} />
      <Field label="Teléfono" keyboardType="phone-pad" placeholder="+54 11 00000000" value={form.phone} onChangeText={set('phone')} onBlur={() => { const formatted = formatPhone(form.phone); if (formatted) set('phone')(formatted); }} />
      <ErrorText text={error} />
      <Button label={busy ? 'Creando…' : 'Crear cuenta'} disabled={busy} onPress={() => void submit()} />
    </Screen>
  );
}
