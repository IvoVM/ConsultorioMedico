import { useState } from 'react';
import { Text } from 'react-native';
import { api } from '../../src/api';
import { colors, type } from '../../src/theme';
import { Button, ErrorText, Field, Screen } from '../../src/ui';

const sample = 'Nombre,Apellido,Email,Rol,Matricula,Especialidad\nAna,Paz,ana.paz@demo.local,Medico,MN 200,Clínica médica';

export default function Empleados() {
  const [csv, setCsv] = useState(sample);
  const [result, setResult] = useState('');
  const [error, setError] = useState('');

  return (
    <Screen title="Empleados" subtitle="Pegá el CSV. El rol puede ser Medico, Secretario o AdminTenant.">
      <Field label="CSV" multiline value={csv} onChangeText={setCsv} style={{ minHeight: 140, textAlignVertical: 'top' }} />
      <ErrorText text={error} />
      {result ? <Text style={{ fontFamily: type.sans, color: colors.ink }}>{result}</Text> : null}
      <Button
        label="Importar"
        onPress={() => {
          setError('');
          api.importEmployees(csv)
            .then((imported) => {
              const created = imported.created.map((item) => `${item.email} · ${item.temporaryPassword}`).join('\n');
              const rejected = imported.rejected.map((item) => `Fila ${item.row}: ${item.reason}`).join('\n');
              setResult([created, rejected].filter(Boolean).join('\n') || 'No hubo filas nuevas.');
            })
            .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'No se pudo importar.'));
        }}
      />
    </Screen>
  );
}
