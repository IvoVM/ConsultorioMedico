import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ApiError, login } from '../src/api';
import { AuthFrom, LoginAccount } from '../src/status';
import { Button, ErrorText, Field, Screen } from '../src/ui';

// The API answers 409 only when the password opens both the staff and the patient account of one document.
const AccountChoice = 409;

export default function Ingreso() {
  const router = useRouter();
  const params = useLocalSearchParams<{ documento?: string; desde?: string }>();
  const [documentNumber, setDocumentNumber] = useState(params.documento ?? '');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [choosing, setChoosing] = useState(false);

  async function submit(account?: LoginAccount) {
    setBusy(true);
    setError('');
    try {
      await login(documentNumber.trim(), password, account);
      router.replace(params.desde === AuthFrom.Solicitud ? '/(tabs)/turnos' : '/(tabs)/panel');
    } catch (reason) {
      if (reason instanceof ApiError && reason.status === AccountChoice) setChoosing(true);
      setError(reason instanceof Error ? reason.message : 'No se pudo ingresar.');
    } finally {
      setBusy(false);
    }
  }

  if (choosing) {
    return (
      <Screen title="Ingreso" subtitle={error}>
        <Button label="Equipo del consultorio" disabled={busy} onPress={() => void submit(LoginAccount.Staff)} />
        <Button label="Paciente" tone="ghost" disabled={busy} onPress={() => void submit(LoginAccount.Patient)} />
        <Button
          label="Usar otro documento"
          tone="ghost"
          disabled={busy}
          onPress={() => {
            setChoosing(false);
            setError('');
            setPassword('');
          }}
        />
      </Screen>
    );
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
