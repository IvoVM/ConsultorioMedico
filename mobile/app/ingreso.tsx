import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { login } from '../src/api';
import { AuthFrom } from '../src/status';
import { Button, ErrorText, Field, Screen } from '../src/ui';

export default function Ingreso() {
  const router = useRouter();
  const params = useLocalSearchParams<{ documento?: string; desde?: string }>();
  const [documentNumber, setDocumentNumber] = useState(params.documento ?? '');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit() {
    setBusy(true);
    setError('');
    try {
      await login(documentNumber.trim(), password);
      router.replace(params.desde === AuthFrom.Solicitud ? '/(tabs)/turnos' : '/(tabs)/panel');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo ingresar.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen title="Ingreso" subtitle="Entrá con el documento de tu cuenta.">
      <Field label="Documento" autoCapitalize="none" value={documentNumber} onChangeText={setDocumentNumber} />
      <Field label="Contraseña" secureTextEntry value={password} onChangeText={setPassword} />
      <ErrorText text={error} />
      <Button label={busy ? 'Entrando…' : 'Entrar'} disabled={busy} onPress={() => void submit()} />
    </Screen>
  );
}
