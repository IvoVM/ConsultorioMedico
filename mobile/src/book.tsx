import { useRouter } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import { Modal, Pressable, Text, View } from 'react-native';
import { formatPhone } from './account';
import { api, hora, type AppointmentType, type Insurer, type Slot, type Specialty, AppointmentKind } from './api';
import { TenantRole } from './role';
import { useSession } from './session';
import { colors, type } from './theme';
import { Button, Empty, ErrorText, Field, Option, Screen } from './ui';

enum Intent {
  Consulta = 1,
  Estudio = 2,
}

enum Coverage {
  Particular = 1,
  Obra = 2,
}

enum Offer {
  Choose = 1,
  Confirm = 2,
}

enum Step {
  Specialty = 1,
  Day = 2,
  Time = 3,
  Data = 4,
  Coverage = 5,
  Review = 6,
  Done = 7,
}

export function BookScreen() {
  const router = useRouter();
  const { current } = useSession();
  const [intent, setIntent] = useState<Intent | null>(null);
  const [step, setStep] = useState<Step>(Step.Specialty);
  const [specialties, setSpecialties] = useState<Specialty[]>([]);
  const [types, setTypes] = useState<AppointmentType[]>([]);
  const [locationIds, setLocationIds] = useState<string[]>([]);
  const [specialtyId, setSpecialtyId] = useState('');
  const [studyId, setStudyId] = useState('');
  const [visitTypeIds, setVisitTypeIds] = useState<string[]>([]);
  const [year, setYear] = useState(new Date().getFullYear());
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [openDays, setOpenDays] = useState<string[]>([]);
  const [dayIso, setDayIso] = useState('');
  const [slots, setSlots] = useState<Slot[]>([]);
  const [slot, setSlot] = useState<Slot | null>(null);
  const [insurers, setInsurers] = useState<Insurer[]>([]);
  const [coverage, setCoverage] = useState<Coverage>(Coverage.Particular);
  const [insurerId, setInsurerId] = useState('');
  const [memberNumber, setMemberNumber] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [documentNumber, setDocumentNumber] = useState('');
  const [phone, setPhone] = useState('');
  const [offer, setOffer] = useState<Offer | null>(null);
  const [nearest, setNearest] = useState<Slot | null>(null);
  const [seeking, setSeeking] = useState(false);
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([api.locations(), api.specialties(), api.types()])
      .then(async ([locations, nextSpecialties, nextTypes]) => {
        setSpecialties(nextSpecialties);
        setTypes(nextTypes);
        setLocationIds(locations.filter((item) => item.isActive).map((item) => item.id));
        if (current?.role === TenantRole.Patient) {
          const history = await api.history().catch(() => null);
          if (!history) return;
          const parts = history.patient.name.trim().split(/\s+/);
          setFirstName(parts[0] ?? '');
          setLastName(parts.slice(1).join(' '));
          setDocumentNumber(history.patient.documentNumber);
          setPhone(formatPhone(history.patient.phone) ?? history.patient.phone);
        }
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo cargar la agenda.'))
      .finally(() => setLoading(false));
  }, [current?.role]);

  const catalog = useMemo(() => {
    if (intent === Intent.Estudio) {
      return types
        .filter((item) => item.specialtyId && item.kind === AppointmentKind.Estudio)
        .map((item) => ({ id: item.id, name: item.name, specialtyId: item.specialtyId ?? '' }))
        .sort((left, right) => left.name.localeCompare(right.name, 'es'));
    }
    const ids = new Set(types.filter((item) => item.specialtyId && item.kind === AppointmentKind.Consulta).map((item) => item.specialtyId));
    return specialties
      .filter((item) => ids.has(item.id))
      .map((item) => ({ id: item.id, name: item.name, specialtyId: item.id }))
      .sort((left, right) => left.name.localeCompare(right.name, 'es'));
  }, [intent, specialties, types]);

  const specialtyName = intent === Intent.Estudio
    ? types.find((item) => item.id === studyId)?.name ?? ''
    : specialties.find((item) => item.id === specialtyId)?.name ?? '';

  const typeIds = intent === Intent.Estudio ? [studyId].filter((id) => id.length > 0) : visitTypeIds;

  function accepts(item: Slot) {
    if (intent === Intent.Estudio) return item.appointmentTypeId === studyId;
    return visitTypeIds.includes(item.appointmentTypeId);
  }

  function resetSchedule() {
    setSlot(null);
    setDayIso('');
    setSlots([]);
    setNearest(null);
    setOpenDays([]);
  }

  function pickIntent(kind: Intent) {
    setIntent(kind);
    setStep(Step.Specialty);
    setOffer(null);
    setSpecialtyId('');
    setStudyId('');
    setVisitTypeIds([]);
    resetSchedule();
    setError('');
  }

  function pickSpecialty(option: { id: string; specialtyId: string }) {
    setSpecialtyId(option.specialtyId);
    if (intent === Intent.Estudio) {
      setStudyId(option.id);
      setVisitTypeIds([]);
    } else {
      setStudyId('');
      setVisitTypeIds(types.filter((item) => item.specialtyId === option.specialtyId && item.kind === AppointmentKind.Consulta).map((item) => item.id));
    }
    resetSchedule();
    setError('');
    setNearest(null);
    setOffer(Offer.Choose);
  }

  async function loadMonth(nextYear = year, nextMonth = month, ids = typeIds, specialty = specialtyId) {
    setLoading(true);
    setError('');
    try {
      const days = await api.openDays(specialty, nextYear, nextMonth, ids);
      setOpenDays(days.map((day) => day.slice(0, 10)));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudieron leer los días.');
    } finally {
      setLoading(false);
    }
  }

  async function slotsFor(iso: string, ids = typeIds, specialty = specialtyId) {
    const lists = await Promise.all(locationIds.map((locationId) => api.availability(locationId, specialty, iso)));
    const now = Date.now();
    return lists.flat().filter((item) => new Date(item.start).getTime() > now && accepts(item)).sort((left, right) => left.start.localeCompare(right.start));
  }

  async function chooseManual() {
    setOffer(null);
    const today = new Date();
    setYear(today.getFullYear());
    setMonth(today.getMonth() + 1);
    setStep(Step.Day);
    await loadMonth(today.getFullYear(), today.getMonth() + 1);
  }

  async function chooseNearest() {
    setSeeking(true);
    setError('');
    try {
      const now = new Date();
      const today = isoDate(now);
      let found: Slot | null = null;
      for (let offset = 0; offset < 4 && !found; offset++) {
        const cursor = new Date(now.getFullYear(), now.getMonth() + offset, 1);
        const days = (await api.openDays(specialtyId, cursor.getFullYear(), cursor.getMonth() + 1, typeIds))
          .map((day) => day.slice(0, 10))
          .filter((day) => day >= today)
          .sort();
        for (const iso of days) {
          const list = await slotsFor(iso);
          if (list[0]) {
            found = list[0];
            break;
          }
        }
      }
      if (!found) {
        await chooseManual();
        setError('No hay un turno próximo. Elegí el día y el horario.');
        return;
      }
      setNearest(found);
      setOffer(Offer.Confirm);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo buscar el turno.');
    } finally {
      setSeeking(false);
    }
  }

  async function applySlot(chosen: Slot) {
    const iso = chosen.start.slice(0, 10);
    const [nextYear, nextMonth] = iso.split('-').map(Number);
    setDayIso(iso);
    setYear(nextYear);
    setMonth(nextMonth);
    setSlot(chosen);
    setCoverage(Coverage.Particular);
    setInsurerId('');
    setMemberNumber('');
    setOffer(null);
    setError('');
    try {
      setInsurers(await api.professionalInsurers(chosen.professionalId));
    } catch {
      setInsurers([]);
    }
    setStep(Step.Data);
  }

  async function pickDay(iso: string) {
    setLoading(true);
    setError('');
    try {
      const list = await slotsFor(iso);
      setDayIso(iso);
      setSlots(list);
      setStep(Step.Time);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudieron leer los horarios.');
    } finally {
      setLoading(false);
    }
  }

  async function shiftMonth(delta: number) {
    const next = new Date(year, month - 1 + delta, 1);
    const today = new Date();
    if (next.getFullYear() < today.getFullYear() || (next.getFullYear() === today.getFullYear() && next.getMonth() < today.getMonth())) return;
    setYear(next.getFullYear());
    setMonth(next.getMonth() + 1);
    await loadMonth(next.getFullYear(), next.getMonth() + 1);
  }

  function continueData() {
    const name = firstName.trim();
    const last = lastName.trim();
    const document = documentNumber.replace(/[.\-\s]/g, '');
    const formatted = formatPhone(phone);
    if (!name || !last) {
      setError('Escribí tu nombre y tu apellido.');
      return;
    }
    if (document.length < 6 || document.length > 20) {
      setError('El documento no es válido.');
      return;
    }
    if (!formatted) {
      setError('El teléfono no es válido.');
      return;
    }
    setFirstName(name);
    setLastName(last);
    setDocumentNumber(document);
    setPhone(formatted);
    setError('');
    setStep(Step.Coverage);
  }

  function pickCoverage(kind: Coverage) {
    setError('');
    if (kind === Coverage.Particular || insurers.length === 0) {
      setCoverage(Coverage.Particular);
      setInsurerId('');
      setMemberNumber('');
      setStep(Step.Review);
      return;
    }
    setCoverage(Coverage.Obra);
  }

  function continueCoverage() {
    if (!insurerId) {
      setError('Elegí tu obra social.');
      return;
    }
    if (!memberNumber.trim()) {
      setError('El número de afiliado es obligatorio.');
      return;
    }
    setMemberNumber(memberNumber.trim());
    setError('');
    setStep(Step.Review);
  }

  async function send() {
    if (!slot) return;
    setSending(true);
    setError('');
    try {
      await api.createRequest({
        specialtyId,
        professionalId: slot.professionalId,
        locationId: slot.locationId,
        appointmentTypeId: slot.appointmentTypeId,
        start: slot.start,
        name: `${firstName.trim()} ${lastName.trim()}`,
        phone: phone.trim(),
        documentNumber: documentNumber.trim(),
        insurerId: coverage === Coverage.Obra ? insurerId : null,
        memberNumber: coverage === Coverage.Obra ? memberNumber.trim() : null,
      });
      setStep(Step.Done);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo enviar la solicitud.');
    } finally {
      setSending(false);
    }
  }

  const question = step === Step.Specialty
    ? intent === Intent.Estudio ? '¿Qué estudio necesitás?' : '¿Qué consulta necesitás?'
    : step === Step.Day ? '¿Qué día te queda bien?'
      : step === Step.Time ? '¿A qué hora?'
        : step === Step.Data ? 'Tus datos'
          : step === Step.Coverage ? '¿Tenés obra social?'
            : step === Step.Review ? 'Revisá la solicitud'
              : 'Listo';

  const hint = step === Step.Specialty
    ? intent === Intent.Estudio
      ? 'Es una práctica. Después podés tomar el turno más próximo o elegir el día.'
      : 'Es para ver a un profesional. Después podés tomar el turno más próximo o elegir el día.'
    : step === Step.Day ? 'Solo aparecen los días con un horario libre.'
      : step === Step.Time ? `${specialtyName}. Elegí un horario.`
        : step === Step.Data ? 'Nombre, apellido, documento y teléfono. Con el documento vas a poder ver esta solicitud.'
          : step === Step.Coverage ? (insurers.length ? 'Si no tenés, el turno queda como particular.' : 'Este profesional atiende como particular.')
            : step === Step.Review ? 'Al enviarla, la secretaría confirma el turno.'
              : current
                ? 'La secretaría puede contactarte para confirmar el turno. También lo vas a ver en Mis turnos.'
                : 'La secretaría puede contactarte para confirmar el turno. Si querés ver el estado del turno y tu información personal, podés crear una cuenta.';

  const cells = useMemo(() => monthCells(year, month, openDays), [year, month, openDays]);
  const today = new Date();
  const canGoBack = year > today.getFullYear() || (year === today.getFullYear() && month > today.getMonth() + 1);
  const monthTitle = `${capitalize(new Date(year, month - 1, 1).toLocaleDateString('es-AR', { month: 'long' }))} ${year}`;
  const insurerName = insurers.find((item) => item.id === insurerId)?.name ?? '';

  return (
    <Screen title="Reservá tu turno" subtitle={intent ? hint : 'Una consulta es ver a un profesional. Un estudio es una práctica, como un electrocardiograma.'}>
      {!intent ? (
        loading ? <Empty text="Cargando servicios…" /> : (
          <>
            <Text style={{ fontFamily: type.serif, fontSize: 28, lineHeight: 32, color: colors.deep }}>¿Qué necesitás?</Text>
            <Option title="Consulta médica" detail="Ver a un profesional" onPress={() => pickIntent(Intent.Consulta)} />
            <Option title="Estudio" detail="Hacerte una práctica o un estudio" onPress={() => pickIntent(Intent.Estudio)} />
          </>
        )
      ) : (
        <>
          <Button label="Cambiar entre consulta y estudio" tone="ghost" onPress={() => { setIntent(null); setOffer(null); setError(''); resetSchedule(); }} />
          {step !== Step.Done ? <Text style={{ fontFamily: type.sansBold, color: colors.ink }}>{question}</Text> : <Text style={{ fontFamily: type.serif, fontSize: 28, lineHeight: 32, color: colors.deep }}>Listo</Text>}
          <ErrorText text={error} />
          {step === Step.Specialty ? (
            loading ? <Empty text="Cargando servicios…" /> : catalog.length === 0 ? (
              <Empty text={intent === Intent.Estudio ? 'Todavía no hay estudios para reservar.' : 'Todavía no hay consultas para reservar.'} />
            ) : catalog.map((item) => (
              <Option key={item.id} title={item.name} selected={item.id === (intent === Intent.Estudio ? studyId : specialtyId)} onPress={() => pickSpecialty(item)} />
            ))
          ) : null}
          {step === Step.Day ? (
            <View style={{ gap: 12 }}>
              <View style={{ gap: 8 }}>
                <Text style={{ fontFamily: type.sansBold, fontSize: 18, color: colors.deep, textAlign: 'center' }}>{monthTitle}</Text>
                <Button label="Mes anterior" tone="ghost" disabled={!canGoBack} onPress={() => void shiftMonth(-1)} />
                <Button label="Mes siguiente" tone="ghost" onPress={() => void shiftMonth(1)} />
              </View>
              <View style={{ flexDirection: 'row' }}>
                {['Lu', 'Ma', 'Mi', 'Ju', 'Vi', 'Sá', 'Do'].map((label) => (
                  <Text key={label} style={{ width: '14.28%', textAlign: 'center', fontFamily: type.sansBold, color: colors.ink }}>{label}</Text>
                ))}
              </View>
              <View style={{ flexDirection: 'row', flexWrap: 'wrap' }}>
                {cells.map((cell, index) => (
                  <View key={cell?.iso ?? `empty-${index}`} style={{ width: '14.28%', aspectRatio: 1, padding: 3 }}>
                    {cell?.open ? (
                      <Pressable
                        accessibilityRole="button"
                        onPress={() => void pickDay(cell.iso)}
                        style={{ flex: 1, borderRadius: 4, alignItems: 'center', justifyContent: 'center', backgroundColor: cell.iso === dayIso ? colors.deep : colors.white, borderWidth: 1, borderColor: colors.clinic }}
                      >
                        <Text style={{ fontFamily: type.sansBold, fontSize: 18, color: cell.iso === dayIso ? colors.white : colors.deep }}>{cell.day}</Text>
                      </Pressable>
                    ) : null}
                  </View>
                ))}
              </View>
              {openDays.length === 0 && !loading ? <Empty text={`Este mes no tiene horarios libres para ${specialtyName}.`} /> : null}
            </View>
          ) : null}
          {step === Step.Time ? (
            slots.length === 0 ? <Empty text="Ese día ya no tiene horarios libres." /> : slots.map((item) => (
              <Option
                key={`${item.professionalId}-${item.start}`}
                title={hora(item.start)}
                detail={item.professional}
                selected={slot?.start === item.start && slot.professionalId === item.professionalId}
                onPress={() => void applySlot(item)}
              />
            ))
          ) : null}
          {step === Step.Data ? (
            <>
              <Field label="Nombre" value={firstName} onChangeText={setFirstName} autoComplete="given-name" />
              <Field label="Apellido" value={lastName} onChangeText={setLastName} autoComplete="family-name" />
              <Field label="Documento" value={documentNumber} onChangeText={setDocumentNumber} maxLength={20} />
              <Field label="Teléfono" keyboardType="phone-pad" placeholder="+54 11 00000000" autoComplete="tel" value={phone} onChangeText={setPhone} onBlur={() => { const formatted = formatPhone(phone); if (formatted) setPhone(formatted); }} />
              <Button label="Continuar" onPress={continueData} />
            </>
          ) : null}
          {step === Step.Coverage ? (
            <>
              <Option title="No, voy como particular" onPress={() => pickCoverage(Coverage.Particular)} />
              {insurers.length ? <Option title="Sí, tengo obra social" selected={coverage === Coverage.Obra} onPress={() => pickCoverage(Coverage.Obra)} /> : null}
              {coverage === Coverage.Obra ? (
                <>
                  <Text style={{ fontFamily: type.sans, fontSize: 16, color: colors.ink }}>Solo las que atiende el profesional de ese horario.</Text>
                  {insurers.map((item) => (
                    <Option key={item.id} title={item.name} selected={insurerId === item.id} onPress={() => setInsurerId(item.id)} />
                  ))}
                  {insurerId ? (
                    <>
                      <Field label="Número de afiliado" value={memberNumber} onChangeText={setMemberNumber} maxLength={40} />
                      <Button label="Continuar" onPress={continueCoverage} />
                    </>
                  ) : null}
                </>
              ) : null}
            </>
          ) : null}
          {step === Step.Review && slot ? (
            <>
              <Summary label="Servicio" value={specialtyName} />
              <Summary label="Cuándo" value={when(slot.start)} />
              <Summary label="Profesional" value={slot.professional} />
              <Summary label="Nombre" value={firstName} />
              <Summary label="Apellido" value={lastName} />
              <Summary label="Documento" value={documentNumber} />
              <Summary label="Teléfono" value={phone} />
              <Summary label="Cobertura" value={coverage === Coverage.Obra ? `${insurerName} · ${memberNumber}` : 'Particular'} />
              <Button label={sending ? 'Enviando…' : 'Enviar solicitud'} disabled={sending} onPress={() => void send()} />
            </>
          ) : null}
          {step === Step.Done ? (
            current ? <Button label="Volver al inicio" onPress={() => router.replace('/(tabs)/panel')} /> : (
              <Button
                label="Crear cuenta y ver el turno"
                onPress={() => router.push({ pathname: '/registro', params: { documento: documentNumber, nombre: firstName, apellido: lastName, telefono: phone } })}
              />
            )
          ) : null}
        </>
      )}
      <Modal visible={offer !== null} transparent animationType="fade" onRequestClose={() => setOffer(null)}>
        <Pressable onPress={() => setOffer(null)} style={{ flex: 1, backgroundColor: 'rgba(23,51,44,0.45)', justifyContent: 'flex-end', padding: 16 }}>
          <Pressable onPress={() => undefined} style={{ backgroundColor: colors.white, borderRadius: 8, padding: 20, gap: 12 }}>
            <Text style={{ fontFamily: type.serif, fontSize: 28, lineHeight: 32, color: colors.deep }}>{offer === Offer.Confirm ? 'El turno más próximo' : '¿Cómo querés el turno?'}</Text>
            <ErrorText text={offer ? error : ''} />
            {offer === Offer.Choose ? (
              seeking ? <Empty text="Buscando el turno más próximo…" /> : (
                <>
                  <Text style={{ fontFamily: type.sans, fontSize: 16, lineHeight: 22, color: colors.ink }}>Podés tomar el primero que haya, o elegir el día y la hora.</Text>
                  <Option title="El más próximo" onPress={() => void chooseNearest()} />
                  <Option title="Elegir día y horario" onPress={() => void chooseManual()} />
                </>
              )
            ) : null}
            {offer === Offer.Confirm && nearest ? (
              <>
                <Text style={{ fontFamily: type.sans, fontSize: 16, lineHeight: 22, color: colors.ink }}>El más próximo es el {when(nearest.start)} con {nearest.professional}. ¿Te parece bien?</Text>
                <Option title="Sí, me parece bien" onPress={() => void applySlot(nearest)} />
                <Option title="No, elijo el día y la hora" onPress={() => void chooseManual()} />
              </>
            ) : null}
          </Pressable>
        </Pressable>
      </Modal>
    </Screen>
  );
}

function Summary({ label, value }: { label: string; value: string }) {
  return (
    <View style={{ gap: 2 }}>
      <Text style={{ fontFamily: type.sans, fontSize: 14, color: colors.ink }}>{label}</Text>
      <Text style={{ fontFamily: type.sansBold, fontSize: 18, color: colors.deep }}>{value}</Text>
    </View>
  );
}

function monthCells(year: number, month: number, openDays: string[]) {
  const first = new Date(year, month - 1, 1);
  const lead = (first.getDay() + 6) % 7;
  const count = new Date(year, month, 0).getDate();
  const open = new Set(openDays);
  const cells: Array<{ iso: string; day: number; open: boolean } | null> = Array.from({ length: lead }, () => null);
  for (let day = 1; day <= count; day++) {
    const iso = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    cells.push({ iso, day, open: open.has(iso) });
  }
  return cells;
}

function isoDate(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function capitalize(value: string) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function when(value: string) {
  const date = new Date(value);
  const label = date.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
  return `${capitalize(label)}, ${hora(value)}`;
}
