import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { api, type AppointmentType, type Location, type Professional, type Service, type Specialty } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, Card, ErrorText, Field, Screen } from '../../src/ui';

export default function Organizacion() {
  const [locations, setLocations] = useState<Location[]>([]);
  const [specialties, setSpecialties] = useState<Specialty[]>([]);
  const [services, setServices] = useState<Service[]>([]);
  const [types, setTypes] = useState<AppointmentType[]>([]);
  const [professionals, setProfessionals] = useState<Professional[]>([]);
  const [locationName, setLocationName] = useState('');
  const [address, setAddress] = useState('');
  const [specialtyName, setSpecialtyName] = useState('');
  const [serviceName, setServiceName] = useState('');
  const [typeName, setTypeName] = useState('');
  const [minutes, setMinutes] = useState('30');
  const [error, setError] = useState('');

  function load() {
    Promise.all([api.locations(), api.specialties(), api.services(), api.types(), api.professionals()])
      .then(([a, b, c, d, e]) => {
        setLocations(a);
        setSpecialties(b);
        setServices(c);
        setTypes(d);
        setProfessionals(e);
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo leer la organización.'));
  }

  useEffect(load, []);

  async function run(task: () => Promise<unknown>) {
    setError('');
    try {
      await task();
      load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo guardar.');
    }
  }

  return (
    <Screen title="Organización" subtitle="Sedes, especialidades, servicios y tipos de turno.">
      <ErrorText text={error} />
      <Field label="Nueva sede" value={locationName} onChangeText={setLocationName} />
      <Field label="Dirección" value={address} onChangeText={setAddress} />
      <Button label="Agregar sede" onPress={() => void run(() => api.saveLocation({ name: locationName, address }))} />
      {locations.map((item) => (
        <Card key={item.id}><Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.name} · {item.address}</Text></Card>
      ))}
      <Field label="Nueva especialidad" value={specialtyName} onChangeText={setSpecialtyName} />
      <Button label="Agregar especialidad" onPress={() => void run(() => api.saveSpecialty({ name: specialtyName }))} />
      {specialties.map((item) => (
        <Card key={item.id}><Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.name}</Text></Card>
      ))}
      <Field label="Nuevo servicio" value={serviceName} onChangeText={setServiceName} />
      <Button label="Agregar servicio en la primera sede" disabled={!locations[0]} onPress={() => void run(() => api.saveService({ locationId: locations[0].id, name: serviceName }))} />
      {services.map((item) => (
        <Card key={item.id}><Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.name}</Text></Card>
      ))}
      <Field label="Tipo de turno" value={typeName} onChangeText={setTypeName} />
      <Field label="Minutos" keyboardType="number-pad" value={minutes} onChangeText={setMinutes} />
      <Button label="Agregar tipo" onPress={() => void run(() => api.saveType({ name: typeName, durationMinutes: Number(minutes), specialtyId: specialties[0]?.id ?? null }))} />
      {types.map((item) => (
        <Card key={item.id}><Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.name} · {item.durationMinutes} min</Text></Card>
      ))}
      {professionals.map((item) => (
        <Card key={item.id}>
          <Text style={{ fontFamily: type.sansBold, color: colors.deep }}>{item.lastName}, {item.firstName}</Text>
          <Text style={{ fontFamily: type.sans, color: colors.ink }}>{item.email}</Text>
          {specialties[0] ? (
            <Button label={`Asignar ${specialties[0].name}`} tone="ghost" onPress={() => void run(() => api.assignSpecialty(item.id, specialties[0].id))} />
          ) : null}
        </Card>
      ))}
    </Screen>
  );
}
