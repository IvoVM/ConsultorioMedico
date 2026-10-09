import { AccessibilityInfo, Pressable, ScrollView, StyleSheet, Text, TextInput, View, type TextInputProps } from 'react-native';
import Animated, { useAnimatedStyle, useSharedValue, withSpring } from 'react-native-reanimated';
import { useEffect, useState, type ReactNode } from 'react';
import { colors, type } from './theme';

const Press = Animated.createAnimatedComponent(Pressable);

let reduceMotion = false;
AccessibilityInfo.isReduceMotionEnabled().then((value) => {
  reduceMotion = value;
});

export function Screen({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
}) {
  return (
    <ScrollView style={styles.screen} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <Text style={styles.title}>{title}</Text>
      {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
      <View style={styles.body}>{children}</View>
    </ScrollView>
  );
}

export function Button({
  label,
  onPress,
  tone = 'clinic',
  disabled,
}: {
  label: string;
  onPress: () => void;
  tone?: 'clinic' | 'ghost' | 'pulse';
  disabled?: boolean;
}) {
  const scale = useSharedValue(1);
  const animated = useAnimatedStyle(() => ({ transform: [{ scale: scale.value }] }));
  return (
    <Press
      accessibilityRole="button"
      disabled={disabled}
      onPress={onPress}
      onPressIn={() => {
        if (!reduceMotion) scale.value = withSpring(0.97, { damping: 18, stiffness: 280 });
      }}
      onPressOut={() => {
        scale.value = withSpring(1, { damping: 16, stiffness: 220 });
      }}
      style={[styles.button, tone === 'ghost' && styles.ghost, tone === 'pulse' && styles.pulse, disabled && styles.disabled, animated]}
    >
      <Text style={[styles.buttonText, tone === 'ghost' && styles.ghostText]}>{label}</Text>
    </Press>
  );
}

export function Field({ label, style, ...props }: { label: string } & TextInputProps) {
  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <TextInput placeholderTextColor="#7d9088" style={[styles.input, style]} {...props} />
    </View>
  );
}

export function Card({ children, onPress }: { children: ReactNode; onPress?: () => void }) {
  if (!onPress) return <View style={styles.card}>{children}</View>;
  return (
    <Pressable onPress={onPress} style={styles.card}>
      {children}
    </Pressable>
  );
}

export function Empty({ text }: { text: string }) {
  return <Text style={styles.empty}>{text}</Text>;
}

export function ErrorText({ text }: { text: string }) {
  return text ? <Text style={styles.error}>{text}</Text> : null;
}

export function Choice({
  label,
  selected,
  onPress,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
}) {
  const scale = useSharedValue(1);
  const animated = useAnimatedStyle(() => ({ transform: [{ scale: scale.value }] }));
  return (
    <Press
      onPress={onPress}
      onPressIn={() => {
        if (!reduceMotion) scale.value = withSpring(0.96, { damping: 18, stiffness: 320 });
      }}
      onPressOut={() => {
        scale.value = withSpring(selected ? 1.02 : 1, { damping: 14, stiffness: 240 });
      }}
      style={[styles.choice, selected && styles.choiceOn, animated]}
    >
      <Text style={[styles.choiceText, selected && styles.choiceTextOn]}>{label}</Text>
    </Press>
  );
}

export function useLoad(task: () => Promise<void>, deps: unknown[] = []) {
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let alive = true;
    setLoading(true);
    task()
      .then(() => {
        if (alive) setError('');
      })
      .catch((reason: unknown) => {
        if (alive) setError(reason instanceof Error ? reason.message : 'No se pudo completar la operación.');
      })
      .finally(() => {
        if (alive) setLoading(false);
      });
    return () => {
      alive = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);
  return { error, setError, loading, setLoading };
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.paper },
  content: { padding: 20, paddingBottom: 40, gap: 8 },
  title: { fontFamily: type.serif, fontSize: 32, color: colors.deep, lineHeight: 36 },
  subtitle: { fontFamily: type.sans, fontSize: 16, color: colors.ink, lineHeight: 22 },
  body: { gap: 12, marginTop: 12 },
  button: { minHeight: 48, borderRadius: 4, backgroundColor: colors.clinic, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 16 },
  ghost: { backgroundColor: 'transparent', borderWidth: 1, borderColor: colors.rule },
  pulse: { backgroundColor: colors.pulse },
  disabled: { opacity: 0.5 },
  buttonText: { fontFamily: type.sansBold, color: colors.white, fontSize: 16 },
  ghostText: { color: colors.deep },
  field: { gap: 6 },
  label: { fontFamily: type.sansBold, color: colors.ink, fontSize: 14 },
  input: {
    minHeight: 48,
    borderRadius: 4,
    backgroundColor: colors.white,
    paddingHorizontal: 12,
    fontFamily: type.sans,
    fontSize: 16,
    color: colors.ink,
    borderWidth: 1,
    borderColor: colors.rule,
  },
  card: { backgroundColor: colors.white, borderRadius: 8, padding: 14, gap: 4, borderWidth: 1, borderColor: colors.rule },
  empty: { fontFamily: type.sans, color: colors.ink, fontSize: 16, lineHeight: 22 },
  error: { fontFamily: type.sans, color: colors.pulse, fontSize: 15, lineHeight: 21 },
  choice: { paddingVertical: 10, paddingHorizontal: 12, borderRadius: 4, backgroundColor: colors.mist },
  choiceOn: { backgroundColor: colors.deep },
  choiceText: { fontFamily: type.sansBold, color: colors.ink },
  choiceTextOn: { color: colors.white },
});
