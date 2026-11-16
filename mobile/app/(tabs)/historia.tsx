import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, fechaCorta, todayIso, type Appointment, type History } from '../../src/api';
import { backgroundLabels, entryTitle } from '../../src/clinical';
import { TenantRole } from '../../src/role';
import { useSession } from '../../src/session';
import { colors, type } from '../../src/theme';
import { Button, Card, Empty, ErrorText, Screen } from '../../src/ui';

// The signed clinical record: background first, then the folios, newest first. Adding background, consents or
// corrections is done from the portal, where the scanned consent can be attached.
export default function Historia() {
  const { current } = useSession();
  const [today, setToday] = useState<Appointment[]>([]);
  const [history, setHistory] = useState<History | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const staff = current?.role === TenantRole.Doctor;
  const patientRole = current?.role === TenantRole.Patient;

  useEffect(() => {
    if (!current) return;
    let alive = true;
    if (current.role === TenantRole.Doctor) {
      api.day(todayIso())
        .then((day) => {
          if (alive) setToday(day);
        })
        .catch(() => undefined);
      return () => {
        alive = false;
      };
    }
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

  async function open(patientId?: string, specialtyId?: string) {
    setError('');
    setLoading(true);
    try {
      setHistory(await api.history(patientId, specialtyId));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo abrir la historia.');
    } finally {
      setLoading(false);
    }
  }

  const patients = [...new Map(today.map((item) => [item.patientId, item])).values()];

  return (
    <Screen title="Historia clínica" subtitle={staff ? 'Abrí la historia de un paciente de hoy.' : 'Tu historia clínica, firmada por cada profesional.'}>
      {staff
        ? patients.map((item) => <Button key={item.patientId} tone="ghost" label={item.patient} onPress={() => void open(item.patientId)} />)
        : null}
      {staff && patients.length === 0 ? <Empty text="No tenés pacientes hoy." /> : null}
      <ErrorText text={error} />
      {loading ? <Empty text="Abriendo la historia…" /> : null}
      {history ? (
        <>
          <Text style={{ fontFamily: type.serif, fontSize: 24, color: colors.deep }}>{history.patient.name}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>
            Historia N.º {history.patient.historyNumber} · DNI {history.patient.documentNumber}
          </Text>
          <Text style={{ fontFamily: type.sansBold, color: history.brokenFolio == null ? colors.deep : colors.ink }}>
            {history.brokenFolio == null ? 'Integridad verificada' : `El folio ${history.brokenFolio} no coincide con su sello`}
          </Text>
          {history.background.map((item) => (
            <Card key={item.kind}>
              <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{backgroundLabels[item.kind]}</Text>
              <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.text}</Text>
            </Card>
          ))}
          {patientRole && history.specialties.length > 1
            ? history.specialties.map((item) => (
                <Button
                  key={item.id}
                  tone={item.id === history.specialty?.id ? undefined : 'ghost'}
                  label={`${item.name} (${item.encounters})`}
                  onPress={() => void open(undefined, item.id)}
                />
              ))
            : null}
          {history.entries.length === 0 ? <Empty text="Todavía no hay folios." /> : null}
          {history.entries.map((item) => (
            <Card key={item.id}>
              <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>
                Folio {item.folio} · {entryTitle(item)} · {fechaCorta(item.recordedAt)}
              </Text>
              <Text style={{ fontFamily: type.sans, color: colors.ink }}>
                {item.signed ? `Firmó ${item.authorName}${item.authorLicense ? `, ${item.authorLicense}` : ''}` : 'Sin firma: cargado antes de la firma electrónica'}
              </Text>
              <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.text}</Text>
              {item.plan ? <Text style={{ fontFamily: type.sans, color: colors.ink }}>Indicaciones: {item.plan}</Text> : null}
              {item.diagnoses ? <Text style={{ fontFamily: type.sans, color: colors.ink }}>CIE-10: {item.diagnoses}</Text> : null}
            </Card>
          ))}
        </>
      ) : null}
    </Screen>
  );
}
