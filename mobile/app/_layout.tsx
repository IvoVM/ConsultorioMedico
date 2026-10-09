import 'react-native-gesture-handler';
import { useFonts } from 'expo-font';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect } from 'react';
import { SourceSans3_400Regular, SourceSans3_700Bold } from '@expo-google-fonts/source-sans-3';
import { SourceSerif4_600SemiBold } from '@expo-google-fonts/source-serif-4';
import { sessionStore } from '../src/session';
import { colors, type } from '../src/theme';

SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  const [fonts] = useFonts({
    [type.serif]: SourceSerif4_600SemiBold,
    [type.sans]: SourceSans3_400Regular,
    [type.sansBold]: SourceSans3_700Bold,
  });

  useEffect(() => {
    void sessionStore.hydrate();
  }, []);

  useEffect(() => {
    if (fonts) void SplashScreen.hideAsync();
  }, [fonts]);

  if (!fonts) return null;

  return (
    <>
      <StatusBar style="dark" />
      <Stack
        screenOptions={{
          headerStyle: { backgroundColor: colors.paper },
          headerTintColor: colors.deep,
          headerTitleStyle: { fontFamily: type.sansBold },
          headerShadowVisible: false,
          contentStyle: { backgroundColor: colors.paper },
        }}
      >
        <Stack.Screen name="index" options={{ headerShown: false }} />
        <Stack.Screen name="ingreso" options={{ title: 'Ingreso' }} />
        <Stack.Screen name="registro" options={{ title: 'Crear cuenta' }} />
        <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
      </Stack>
    </>
  );
}
