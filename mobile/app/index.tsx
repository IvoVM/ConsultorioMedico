import { useRouter } from 'expo-router';
import { useEffect } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Animated, { FadeIn } from 'react-native-reanimated';
import { api } from '../src/api';
import { useSession } from '../src/session';
import { colors, type } from '../src/theme';
import { Button } from '../src/ui';
import { useState } from 'react';

export default function Welcome() {
  const router = useRouter();
  const { current, ready } = useSession();
  const [clinic, setClinic] = useState('Consultorio');

  useEffect(() => {
    if (ready && current) router.replace('/(tabs)/panel');
  }, [ready, current, router]);

  useEffect(() => {
    api.clinic().then((profile) => setClinic(profile.name)).catch(() => undefined);
  }, []);

  return (
    <View style={styles.page}>
      <Animated.View entering={FadeIn.duration(500)} style={styles.mark} />
      <Text style={styles.name}>{clinic}</Text>
      <Text style={styles.lead}>Reservá un turno, mirá tu historia y seguí la atención del día.</Text>
      <Button label="Reservar turno" onPress={() => router.push('/reservar')} />
      <Button label="Entrar" tone="ghost" onPress={() => router.push('/ingreso')} />
      <Button label="Crear cuenta de paciente" tone="ghost" onPress={() => router.push('/registro')} />
    </View>
  );
}

const styles = StyleSheet.create({
  page: { flex: 1, backgroundColor: colors.paper, justifyContent: 'flex-end', padding: 24, gap: 12, paddingBottom: 48 },
  mark: { width: 56, height: 56, borderRadius: 28, backgroundColor: colors.clinic, marginBottom: 8 },
  name: { fontFamily: type.serif, fontSize: 40, lineHeight: 44, color: colors.deep },
  lead: { fontFamily: type.sans, fontSize: 17, lineHeight: 24, color: colors.ink, marginBottom: 12 },
});
