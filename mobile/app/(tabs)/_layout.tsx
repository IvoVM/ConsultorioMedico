import { Tabs, useRouter, useSegments } from 'expo-router';
import { useEffect } from 'react';
import { useSession } from '../../src/session';
import { colors, type } from '../../src/theme';

export default function TabsLayout() {
  const { current, ready } = useSession();
  const router = useRouter();
  const segments = useSegments();
  const booking = segments.includes('reservar');

  useEffect(() => {
    if (ready && !current && !booking) router.replace('/');
  }, [ready, current, booking, router]);

  return (
    <Tabs
      screenOptions={{
        headerStyle: { backgroundColor: colors.paper },
        headerTintColor: colors.deep,
        headerTitleStyle: { fontFamily: type.sansBold },
        headerShadowVisible: false,
        tabBarActiveTintColor: colors.deep,
        tabBarInactiveTintColor: '#7d9088',
        tabBarStyle: { backgroundColor: colors.paper, borderTopColor: colors.rule },
        tabBarLabelStyle: { fontFamily: type.sansBold, fontSize: 12 },
        sceneStyle: { backgroundColor: colors.paper },
      }}
    >
      <Tabs.Screen name="panel" options={{ title: 'Hoy' }} />
      <Tabs.Screen name="reservar" options={{ title: 'Reservar' }} />
      <Tabs.Screen name="mas" options={{ title: 'Más' }} />
      <Tabs.Screen name="turnos" options={{ href: null, title: 'Mis turnos' }} />
      <Tabs.Screen name="historia" options={{ href: null, title: 'Historia' }} />
      <Tabs.Screen name="recetas" options={{ href: null, title: 'Recetas' }} />
      <Tabs.Screen name="secretaria" options={{ href: null, title: 'Mostrador' }} />
      <Tabs.Screen name="medico" options={{ href: null, title: 'Atención' }} />
      <Tabs.Screen name="organizacion" options={{ href: null, title: 'Organización' }} />
      <Tabs.Screen name="pacientes" options={{ href: null, title: 'Pacientes' }} />
      <Tabs.Screen name="empleados" options={{ href: null, title: 'Empleados' }} />
      <Tabs.Screen name="agendas" options={{ href: null, title: 'Agendas' }} />
      <Tabs.Screen name="historias" options={{ href: null, title: 'Historias' }} />
      <Tabs.Screen name="aranceles" options={{ href: null, title: 'Aranceles' }} />
      <Tabs.Screen name="comprobantes" options={{ href: null, title: 'Comprobantes' }} />
      <Tabs.Screen name="auditoria" options={{ href: null, title: 'Auditoría' }} />
    </Tabs>
  );
}
