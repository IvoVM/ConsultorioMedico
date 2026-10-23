import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, fechaCorta, type History } from '../../src/api';
import { TenantRole } from '../../src/role';
import { useSession } from '../../src/session';
import { colors, type } from '../../src/theme';
import { Button, Card, Empty, ErrorText, Field, Screen } from '../../src/ui';

export default function Historia() {
  const { current } = useSession();
  const [query, setQuery] = useState('');
  const [matches, setMatches] = useState<{ patientId: string; firstName: string; lastName: string }[]>([]);
  const [history, setHistory] = useState<History | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const staff = current && current.role !== TenantRole.Patient;

  useEffect(() => {
    if (!current || current.role !== TenantRole.Patient) return;
    let alive = true;
    setLoading(true);
    api.history()
      .then((next) => {
        if (alive) setHistory(next);
      })
      .catch((reason: unknown) => {
        if (alive) setError(reason instanceof Error ? reason.message : 'No se pudo abrir la historia.');
      })
      .finally(() => {
        if (alive) setLoading(false);
      });
    return () => {
      alive = false;
    };
  }, [current]);

  async function open(patientId: string) {
    setError('');
    setLoading(true);
    try {
      setHistory(await api.history(patientId));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo abrir la historia.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Screen title="Historia clínica" subtitle={staff ? 'Buscá al paciente por nombre o documento.' : 'Tu historia y las recetas del consultorio.'}>
      {staff ? (
        <>
          <Field label="Paciente" value={query} onChangeText={setQuery} />
          <Button
            label="Buscar"
            onPress={() => {
              api.searchPatients(query.trim()).then(setMatches).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Sin resultados.'));
            }}
          />
          {matches.map((item) => (
            <Button key={item.patientId} tone="ghost" label={`${item.lastName}, ${item.firstName}`} onPress={() => void open(item.patientId)} />
          ))}
        </>
      ) : null}
      <ErrorText text={error} />
      {loading ? <Empty text="Abriendo la historia…" /> : null}
      {history ? (
        <>
          <Text style={{ fontFamily: type.serif, fontSize: 24, color: colors.deep }}>{history.patient.name}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{history.patient.documentNumber} · {history.patient.phone}</Text>
          {history.encounters.length === 0 ? <Empty text="Todavía no hay encuentros." /> : null}
          {history.encounters.map((item) => (
            <Card key={item.id}>
              <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{fechaCorta(item.createdAt)} {item.isClosed ? '· cerrado' : ''}</Text>
              <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.note || 'Sin nota'}</Text>
            </Card>
          ))}
        </>
      ) : null}
    </Screen>
  );
}
