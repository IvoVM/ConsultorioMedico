import type { Slot } from './api';

let slot: Slot | null = null;

export const hold = {
  get: () => slot,
  set: (next: Slot | null) => {
    slot = next;
  },
};
