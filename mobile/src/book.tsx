import { useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import Animated, { LinearTransition } from 'react-native-reanimated';
import { api, hora, todayIso, type Location, type Professional, type Slot, type Specialty } from './api';
import { hold } from './hold';
import { useSession } from './session';
import { colors, type } from './theme';
import { Button, Choice, Empty, ErrorText, Field, Screen } from './ui';

export function BookScreen() {
  const router = useRouter();
  const { current } = useSession();
  const [locations, setLocations] = useState<Location[]>([]);
  const [specialties, setSpecialties] = useState<Specialty[]>([]);
  const [professionals, setProfessionals] = useState<Professional[]>([]);
  const [locationId, setLocationId] = useState('');
  const [specialtyId, setSpecialtyId] = useState('');
  const [professionalId, setProfessionalId] = useState('');
  const [date, setDate] = useState(todayIso());
  const [slots, setSlots] = useState<Slot[]>([]);
  const [selected, setSelected] = useState<Slot | null>(hold.get());
  const [patientId, setPatientId] = useState('');
  const [query, setQuery] = useState('');
  const [matches, setMatches] = useState<{ patientId: string; firstName: string; lastName: string }[]>([]);
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const staff = current && current.role !== 'Patient';

  useEffect(() => {
    Promise.all([api.locations(), api.specialties(), api.professionals()])
      .then(([nextLocations, nextSpecialties, nextProfessionals]) => {
        setLocations(nextLocations.filter((item) => item.isActive));
        setSpecialties(nextSpecialties);
        setProfessionals(nextProfessionals);
        setLocationId(nextLocations.find((item) => item.isActive)?.id ?? '');
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo cargar la agenda.'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!locationId || !date) return;
    api.availability(locationId, specialtyId, date, professionalId || undefined)
      .then(setSlots)
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No hay horarios.'));
  }, [locationId, specialtyId, professionalId, date]);

  async function search() {
    if (query.trim().length < 2) return;
    setMatches(await api.searchPatients(query.trim()));
  }

  async function confirm() {
    if (!selected) return;
    if (!current) {
      hold.set(selected);
      router.push('/ingreso');
      return;
    }
    if (staff && !patientId) {
      setError('Elegí el paciente.');
      return;
    }
    setError('');
    try {
      await api.book({
        patientId: staff ? patientId : null,
        professionalId: selected.professionalId,
        locationId: selected.locationId,
        appointmentTypeId: selected.appointmentTypeId,
        start: selected.start,
        visitReason: reason || null,
      });
      hold.set(null);
      setSelected(null);
      router.replace(current.role === 'Patient' ? '/(tabs)/turnos' : '/(tabs)/panel');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo reservar.');
    }
  }

  const visible = professionals.filter((item) => !specialtyId || item.specialtyId === specialtyId);

  return (
    <Screen title="Reservar" subtitle={loading ? 'Buscando sedes…' : 'Elegí sede, día y un horario libre.'}>
      <Field label="Día" value={date} onChangeText={setDate} placeholder="AAAA-MM-DD" />
      <Text style={{ fontFamily: type.sansBold, color: colors.ink }}>Sede</Text>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        {locations.map((item) => (
          <Choice key={item.id} label={item.name} selected={item.id === locationId} onPress={() => setLocationId(item.id)} />
        ))}
      </View>
      <Text style={{ fontFamily: type.sansBold, color: colors.ink }}>Especialidad</Text>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        <Choice label="Todas" selected={!specialtyId} onPress={() => setSpecialtyId('')} />
        {specialties.map((item) => (
          <Choice key={item.id} label={item.name} selected={item.id === specialtyId} onPress={() => setSpecialtyId(item.id)} />
        ))}
      </View>
      <Text style={{ fontFamily: type.sansBold, color: colors.ink }}>Profesional</Text>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        <Choice label="Cualquiera" selected={!professionalId} onPress={() => setProfessionalId('')} />
        {visible.map((item) => (
          <Choice key={item.id} label={`${item.firstName} ${item.lastName}`} selected={item.id === professionalId} onPress={() => setProfessionalId(item.id)} />
        ))}
      </View>
      {slots.length === 0 ? <Empty text="No hay turnos libres ese día." /> : null}
      <Animated.View layout={LinearTransition.springify().damping(18)} style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        {slots.map((slot) => (
          <Choice
            key={`${slot.professionalId}-${slot.start}`}
            label={`${hora(slot.start)} ${slot.professional}`}
            selected={selected?.start === slot.start && selected.professionalId === slot.professionalId}
            onPress={() => setSelected(slot)}
          />
        ))}
      </Animated.View>
      <Field label="Motivo" value={reason} onChangeText={setReason} />
      {staff ? (
        <>
          <Field label="Buscar paciente" value={query} onChangeText={setQuery} onSubmitEditing={() => void search()} />
          <Button label="Buscar" tone="ghost" onPress={() => void search()} />
          {matches.map((item) => (
            <Choice
              key={item.patientId}
              label={`${item.lastName}, ${item.firstName}`}
              selected={patientId === item.patientId}
              onPress={() => setPatientId(item.patientId)}
            />
          ))}
        </>
      ) : null}
      <ErrorText text={error} />
      <Button label={current ? 'Confirmar turno' : 'Entrar para confirmar'} disabled={!selected} onPress={() => void confirm()} />
    </Screen>
  );
}
