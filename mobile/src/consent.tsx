import { useEffect, useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { api, type PrivacyNotice } from './api';
import { colors, type } from './theme';

export const patientConsentLabel = 'Leí el aviso de privacidad y acepto que el consultorio use mis datos para darme turnos y atenderme.';
export const staffConsentLabel = 'Le conté al paciente el aviso de privacidad y aceptó que carguemos sus datos.';

// Loads the clinic's privacy notice; its version is what the person agrees to.
export function usePrivacyNotice() {
  const [notice, setNotice] = useState<PrivacyNotice | null>(null);
  useEffect(() => {
    api.privacyNotice().then(setNotice).catch(() => setNotice(null));
  }, []);
  return notice;
}

// A box to agree to the privacy notice, with a link that opens the full text.
export function Consent({
  notice,
  accepted,
  onChange,
  label = patientConsentLabel,
}: {
  notice: PrivacyNotice | null;
  accepted: boolean;
  onChange: (value: boolean) => void;
  label?: string;
}) {
  const [reading, setReading] = useState(false);
  return (
    <View style={styles.row}>
      <Pressable
        accessibilityRole="checkbox"
        accessibilityState={{ checked: accepted, disabled: !notice }}
        disabled={!notice}
        onPress={() => onChange(!accepted)}
        style={[styles.box, accepted && styles.boxOn]}
      >
        {accepted ? <Text style={styles.tick}>✓</Text> : null}
      </Pressable>
      <View style={{ flex: 1, gap: 4 }}>
        <Text style={styles.text} onPress={() => notice && onChange(!accepted)}>{label}</Text>
        <Text accessibilityRole="link" style={styles.link} onPress={() => notice && setReading(true)}>Leer el aviso de privacidad</Text>
      </View>
      <Modal visible={reading && !!notice} animationType="slide" onRequestClose={() => setReading(false)}>
        <ScrollView contentContainerStyle={styles.sheet}>
          <Text style={styles.title}>Aviso de privacidad</Text>
          {notice?.sections.map((section) => (
            <View key={section.title} style={{ gap: 4 }}>
              <Text style={styles.heading}>{section.title}</Text>
              <Text style={styles.text}>{section.body}</Text>
            </View>
          ))}
          <Text style={styles.version}>{notice ? `${notice.clinic} · versión ${notice.version}` : ''}</Text>
          <Text accessibilityRole="button" style={styles.link} onPress={() => setReading(false)}>Cerrar</Text>
        </ScrollView>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: 12, alignItems: 'flex-start' },
  box: { width: 26, height: 26, borderRadius: 4, borderWidth: 2, borderColor: colors.clinic, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.white },
  boxOn: { backgroundColor: colors.clinic },
  tick: { color: colors.white, fontFamily: type.sansBold, fontSize: 16, lineHeight: 18 },
  text: { fontFamily: type.sans, fontSize: 15, lineHeight: 21, color: colors.ink },
  link: { fontFamily: type.sansBold, fontSize: 15, color: colors.clinic, textDecorationLine: 'underline' },
  sheet: { padding: 20, paddingTop: 48, gap: 16, backgroundColor: colors.paper },
  title: { fontFamily: type.serif, fontSize: 28, lineHeight: 32, color: colors.deep },
  heading: { fontFamily: type.sansBold, fontSize: 16, color: colors.deep },
  version: { fontFamily: type.sans, fontSize: 13, color: colors.ink },
});
