import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api } from '../../src/api';
import { formatPhone } from '../../src/account';
import { sessionStore, useSession } from '../../src/session';
import { colors, type } from '../../src/theme';
import { Button, ErrorText, Field, Screen } from '../../src/ui';

export default function Perfil() {
  const { current } = useSession();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [documentNumber, setDocumentNumber] = useState('');
  const [phone, setPhone] = useState('');
  const [error, setError] = useState('');
  const [saved, setSaved] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let alive = true;
    api.profile()
      .then((profile) => {
        if (!alive) return;
        setFirstName(profile.firstName);
        setLastName(profile.lastName);
        setDocumentNumber(profile.documentNumber);
        setPhone(formatPhone(profile.phone) ?? profile.phone);
      })
      .catch((reason: unknown) => {
        if (alive) setError(reason instanceof Error ? reason.message : 'No se pudo leer el perfil.');
      })
      .finally(() => {
        if (alive) setLoading(false);
      });
    return () => {
      alive = false;
    };
  }, []);

  async function save() {
    const formatted = formatPhone(phone);
    if (formatted) setPhone(formatted);
    setSaving(true);
    setError('');
    setSaved('');
    try {
      const profile = await api.updateProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: formatted ?? phone,
      });
      setFirstName(profile.firstName);
      setLastName(profile.lastName);
      setDocumentNumber(profile.documentNumber);
      setPhone(formatPhone(profile.phone) ?? profile.phone);
      if (current) await sessionStore.save({ ...current, name: `${profile.firstName} ${profile.lastName}`.trim() });
      setSaved('Guardamos tus datos.');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudieron guardar los datos.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Screen title="Tu perfil" subtitle="Nombre, apellido y teléfono. El documento identifica tu ficha y no se cambia.">
      <ErrorText text={error} />
      {loading ? null : (
        <>
          <Field label="Nombre" value={firstName} onChangeText={setFirstName} autoComplete="given-name" />
          <Field label="Apellido" value={lastName} onChangeText={setLastName} autoComplete="family-name" />
          <Field label="Documento" value={documentNumber} editable={false} />
          <Field label="Teléfono" value={phone} onChangeText={setPhone} keyboardType="phone-pad" placeholder="+54 11 00000000" autoComplete="tel" />
          {saved ? <Text style={{ fontFamily: type.sans, color: colors.deep }}>{saved}</Text> : null}
          <Button label={saving ? 'Guardando…' : 'Guardar'} disabled={saving} onPress={() => void save()} />
        </>
      )}
    </Screen>
  );
}
