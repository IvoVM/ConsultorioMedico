import { Component, ElementRef, HostListener, computed, forwardRef, inject, input, signal, viewChild } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { UiIcon } from './ui-icon.component';

export interface SelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'ui-select',
  imports: [UiIcon],
  templateUrl: './ui-select.component.html',
  styleUrl: './ui-select.component.css',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiSelect), multi: true }],
})
export class UiSelect implements ControlValueAccessor {
  private readonly host = inject(ElementRef<HTMLElement>);

  private readonly list = viewChild<ElementRef<HTMLUListElement>>('list');
  private readonly trigger = viewChild<ElementRef<HTMLButtonElement>>('trigger');
  private typeBuffer = '';
  private typeTimer = 0;
  private suppressToggle = false;
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;
  readonly listId = `ui-select-${crypto.randomUUID()}`;
  readonly options = input<SelectOption[]>([]);
  readonly placeholder = input('Elegir');
  readonly current = signal('');
  readonly open = signal(false);
  readonly active = signal(0);
  readonly disabled = signal(false);
  readonly label = computed(() => this.options().find((option) => option.value === this.current())?.label ?? '');
  readonly empty = computed(() => !this.label());
  readonly up = signal(false);

  writeValue(value: string | null) {
    this.current.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void) {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void) {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean) {
    this.disabled.set(disabled);
  }

  toggle() {
    if (this.disabled() || this.suppressToggle) return;
    if (this.open()) this.close();
    else this.openPanel();
  }

  beginPick(event: Event, option: SelectOption) {
    event.preventDefault();
    event.stopPropagation();
    this.suppressToggle = true;
    this.choose(option);
    window.setTimeout(() => {
      this.suppressToggle = false;
    });
  }

  onTriggerKey(event: KeyboardEvent) {
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp' && event.key !== 'Enter' && event.key !== ' ') return;
    event.preventDefault();
    this.openPanel();
  }

  onListKey(event: KeyboardEvent) {
    const items = this.options();
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.active.update((index) => Math.min(items.length - 1, index + 1));
      this.revealActive();
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.active.update((index) => Math.max(0, index - 1));
      this.revealActive();
    } else if (event.key === 'Home') {
      event.preventDefault();
      this.active.set(0);
      this.revealActive();
    } else if (event.key === 'End') {
      event.preventDefault();
      this.active.set(Math.max(0, items.length - 1));
      this.revealActive();
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const option = items[this.active()];
      if (option) this.choose(option);
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.close(true);
    } else if (event.key.length === 1 && event.key !== ' ') {
      this.typeAhead(event.key);
    }
  }

  choose(option: SelectOption) {
    this.current.set(option.value);
    this.onChange(option.value);
    this.onTouched();
    this.close(true);
  }

  @HostListener('document:click', ['$event'])
  closeFromDocument(event: MouseEvent) {
    if (!this.open()) return;
    if (this.host.nativeElement.contains(event.target as Node)) return;
    this.close();
  }

  private openPanel() {
    if (this.disabled()) return;
    const selected = this.options().findIndex((option) => option.value === this.current());
    this.active.set(selected >= 0 ? selected : 0);
    const trigger = this.trigger()?.nativeElement;
    if (trigger) {
      const rect = trigger.getBoundingClientRect();
      const below = window.innerHeight - rect.bottom;
      this.up.set(below < 160 && rect.top > below + 80);
    }
    this.open.set(true);
    queueMicrotask(() => {
      this.list()?.nativeElement.focus();
      this.revealActive();
    });
  }

  private revealActive() {
    const list = this.list()?.nativeElement;
    const item = list?.querySelector<HTMLElement>('.is-active');
    if (!list || !item) return;
    const top = item.offsetTop;
    const bottom = top + item.offsetHeight;
    if (top < list.scrollTop) list.scrollTop = top;
    else if (bottom > list.scrollTop + list.clientHeight) list.scrollTop = bottom - list.clientHeight;
  }

  private close(focusTrigger = false) {
    this.open.set(false);
    this.onTouched();
    if (focusTrigger) queueMicrotask(() => this.trigger()?.nativeElement.focus());
  }

  private typeAhead(key: string) {
    window.clearTimeout(this.typeTimer);
    this.typeBuffer = `${this.typeBuffer}${key}`.toLowerCase();
    this.typeTimer = window.setTimeout(() => {
      this.typeBuffer = '';
    }, 500);
    const items = this.options();
    const start = this.active() + 1;
    const ordered = [...items.slice(start), ...items.slice(0, start)];
    const match = ordered.find((option) => option.label.toLowerCase().startsWith(this.typeBuffer));
    if (!match) return;
    this.active.set(items.indexOf(match));
    this.revealActive();
  }
}
