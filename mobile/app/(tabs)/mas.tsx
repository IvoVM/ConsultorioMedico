import { useRouter } from 'expo-router';
import { useSession } from '../../src/session';
import { Button, Screen } from '../../src/ui';

const links: Record<string, { href: string; label: string }[]> = {
  Patient: [
    { href: '/(tabs)/turnos', label: 'Mis turnos' },
    { href: '/(tabs)/historia', label: 'Historia clínica' },
    { href: '/(tabs)/recetas', label: 'Recetas' },
  ],
  Doctor: [
    { href: '/(tabs)/medico', label: 'Atención' },
    { href: '/(tabs)/historia', label: 'Historia clínica' },
  ],
  Secretary: [
    { href: '/(tabs)/secretaria', label: 'Mostrador' },
    { href: '/(tabs)/historia', label: 'Historia clínica' },
  ],
  TenantAdmin: [
    { href: '/(tabs)/secretaria', label: 'Mostrador' },
    { href: '/(tabs)/organizacion', label: 'Organización' },
    { href: '/(tabs)/pacientes', label: 'Pacientes' },
    { href: '/(tabs)/empleados', label: 'Empleados' },
    { href: '/(tabs)/agendas', label: 'Agendas' },
    { href: '/(tabs)/historias', label: 'Historias clínicas' },
    { href: '/(tabs)/aranceles', label: 'Aranceles' },
    { href: '/(tabs)/comprobantes', label: 'Comprobantes' },
    { href: '/(tabs)/auditoria', label: 'Auditoría' },
  ],
};

export default function Mas() {
  const { current } = useSession();
  const router = useRouter();
  const items = links[current?.role ?? ''] ?? [];
  return (
    <Screen title="Más" subtitle="Las mismas tareas que en el escritorio.">
      {items.map((item) => (
        <Button key={item.href} label={item.label} tone="ghost" onPress={() => router.push(item.href as never)} />
      ))}
    </Screen>
  );
}
