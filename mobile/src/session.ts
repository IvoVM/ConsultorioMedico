import * as SecureStore from 'expo-secure-store';
import { useSyncExternalStore } from 'react';

export type Session = {
  token: string;
  refresh: string;
  role: string;
  name: string;
  email: string;
};

const keys = {
  token: 'clinica.token',
  refresh: 'clinica.refresh',
  role: 'clinica.role',
  name: 'clinica.name',
  email: 'clinica.email',
};

let current: Session | null = null;
let ready = false;
const listeners = new Set<() => void>();

function emit() {
  for (const listener of listeners) listener();
}

async function read(key: string) {
  try {
    return await SecureStore.getItemAsync(key);
  } catch {
    return null;
  }
}

async function write(key: string, value: string | null) {
  try {
    if (value) await SecureStore.setItemAsync(key, value);
    else await SecureStore.deleteItemAsync(key);
  } catch {
    // El simulador web puede no tener almacén seguro. La sesión queda en memoria.
  }
}

export const sessionStore = {
  get: () => current,
  isReady: () => ready,
  async hydrate() {
    const token = await read(keys.token);
    const refresh = await read(keys.refresh);
    const role = await read(keys.role);
    const name = await read(keys.name);
    const email = await read(keys.email);
    current = token && refresh && role && name && email ? { token, refresh, role, name, email } : null;
    ready = true;
    emit();
  },
  async save(session: Session) {
    current = session;
    await Promise.all([
      write(keys.token, session.token),
      write(keys.refresh, session.refresh),
      write(keys.role, session.role),
      write(keys.name, session.name),
      write(keys.email, session.email),
    ]);
    emit();
  },
  async clear() {
    current = null;
    await Promise.all(Object.values(keys).map((key) => write(key, null)));
    emit();
  },
};

export function useSession() {
  const value = useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => current,
    () => null,
  );
  const hydrated = useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => ready,
    () => false,
  );
  return { current: value, ready: hydrated };
}
