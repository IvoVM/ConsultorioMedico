import { Injectable, signal } from '@angular/core';

export type ToastTone = 'success' | 'error';

export interface ToastItem {
  id: number;
  tone: ToastTone;
  title: string;
  description: string;
  leaving: boolean;
}

const stayMs = 5000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  private readonly timers = new Map<number, number>();
  readonly items = signal<ToastItem[]>([]);

  success(title: string, description: string) {
    this.push('success', title, description);
  }

  error(title: string, description: string) {
    this.push('error', title, description);
  }

  dismiss(id: number) {
    const current = this.items().find((item) => item.id === id);
    if (!current || current.leaving) return;
    this.clear(id);
    this.items.update((list) => list.map((item) => (item.id === id ? { ...item, leaving: true } : item)));
    this.timers.set(id, window.setTimeout(() => {
      this.timers.delete(id);
      this.items.update((list) => list.filter((item) => item.id !== id));
    }, this.leaveMs()));
  }

  private push(tone: ToastTone, title: string, description: string) {
    const id = ++this.nextId;
    this.items.update((list) => [...list, { id, tone, title, description, leaving: false }]);
    this.timers.set(id, window.setTimeout(() => this.dismiss(id), stayMs));
  }

  private clear(id: number) {
    const timer = this.timers.get(id);
    if (timer === undefined) return;
    window.clearTimeout(timer);
    this.timers.delete(id);
  }

  private leaveMs() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 280;
  }
}
