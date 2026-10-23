import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { formatPhone, passwordIssue } from '../src/account';
import { register } from '../src/api';
import { Button, ErrorText, Field, Screen } from '../src/ui';

export default function Registro() {
  const router = useRouter();
  const params = useLocalSearchParams<{ documento?: string; nombre?: string; apellido?: string; telefono?: string }>();
  const [form, setForm] = useState({
    password: '',
    confirm: '',
    firstName: params.nombre ?? '',
    lastName: params.apellido ?? '',
    documentNumber: params.documento ?? '',
    phone: params.telefono ?? '',
  });
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
    const documentNumber = form.documentNumber.replace(/[.\-\s]/g, '');
    if (documentNumber.length < 6 || documentNumber.length > 20) {
      setError('El documento no es válido.');
      return;
    }
    setBusy(true);
    try {
      await register({
        password: form.password,
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        documentNumber,
        phone,
      });
      router.replace('/(tabs)/panel');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo crear la cuenta.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen title="Cuenta de paciente" subtitle="Entrá después con el documento. El teléfono va como +54 11 00000000.">
      <Field label="Nombre" value={form.firstName} onChangeText={set('firstName')} />
      <Field label="Apellido" value={form.lastName} onChangeText={set('lastName')} />
      <Field label="Documento" autoComplete="username" value={form.documentNumber} onChangeText={set('documentNumber')} />
      <Field label="Teléfono" keyboardType="phone-pad" placeholder="+54 11 00000000" autoComplete="tel" value={form.phone} onChangeText={set('phone')} onBlur={() => { const formatted = formatPhone(form.phone); if (formatted) set('phone')(formatted); }} />
      <Field label="Contraseña" secureTextEntry value={form.password} onChangeText={set('password')} />
      <Field label="Confirmar contraseña" secureTextEntry value={form.confirm} onChangeText={set('confirm')} />
      <ErrorText text={error} />
      <Button label={busy ? 'Creando…' : 'Crear cuenta'} disabled={busy} onPress={() => void submit()} />
    </Screen>
  );
}
