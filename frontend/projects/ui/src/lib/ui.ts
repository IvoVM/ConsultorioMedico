import { Component, computed, HostListener, input, output, signal } from '@angular/core';

@Component({
  selector: 'ui-page',
  template: `
    <div class="app-frame" [attr.data-frame]="frame()" [class.menu-open]="menuOpen()">
      @if (menuOpen()) {
        <button class="menu-backdrop backdrop-fade" type="button" aria-label="Cerrar menú" (click)="menuOpen.set(false)"></button>
      }
      <aside id="app-sidebar" class="app-sidebar" [class.is-open]="menuOpen()" (click)="onSidebarClick($event)">
        <div class="flex items-center gap-3 px-5 pt-6">
          <img src="logo.svg" alt="" class="logo-mark h-11 w-11" />
          <div>
            <p class="font-serif text-2xl leading-none">Clínica</p>
            <p class="mt-1 text-sm text-white/60">{{ eyebrow() }}</p>
          </div>
        </div>
        <div class="mt-8 flex min-h-0 flex-1 flex-col px-3 pb-5">
          <ng-content select="[uiNav]" />
        </div>
      </aside>
      <div class="brand-slot">
        <ng-content select="[uiBrand]" />
      </div>
      <div class="frame-main">
        <header class="shell-bar">
          <button
            class="menu-button"
            type="button"
            aria-label="Abrir menú"
            aria-controls="app-sidebar"
            [attr.aria-expanded]="menuOpen()"
            (click)="menuOpen.set(true)"
          >
            <span class="menu-glyph" [class.is-open]="menuOpen()"></span>
          </button>
          <h1 class="text-balance font-serif text-2xl leading-tight tracking-tight">{{ title() }}</h1>
        </header>
        <header class="entry-bar">
          <div>
            <p class="font-serif text-2xl leading-none text-clinic-deep">Clínica</p>
            <h1 class="mt-2 font-serif text-2xl tracking-tight">{{ title() }}</h1>
            <p class="mt-1 text-sm text-ink/60">{{ eyebrow() }}</p>
          </div>
        </header>
        <main class="page-stage">
          <ng-content />
        </main>
      </div>
    </div>
  `,
})
export class UiPage {
  readonly eyebrow = input('Clínica');
  readonly title = input('');
  readonly frame = input<'shell' | 'auth' | 'entry'>('entry');
  readonly menuOpen = signal(false);

  @HostListener('document:keydown.escape')
  closeMenu() {
    this.menuOpen.set(false);
  }

  onSidebarClick(event: Event) {
    const target = event.target as HTMLElement | null;
    if (target?.closest('a, button')) this.menuOpen.set(false);
  }
}

@Component({
  selector: 'ui-button',
  host: { class: 'inline-flex' },
  template: `
    <button [attr.type]="type()" [disabled]="disabled()" [class]="classes()">
      <ng-content />
    </button>
  `,
})
export class UiButton {
  readonly type = input<'button' | 'submit'>('button');
  readonly variant = input<'primary' | 'ghost' | 'danger'>('primary');
  readonly disabled = input(false);
  readonly classes = computed(() => {
    const base =
      'inline-flex w-full min-h-10 cursor-pointer items-center justify-center rounded-sm px-3.5 py-2 text-sm transition duration-200 hover:-translate-y-px active:translate-y-0 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:translate-y-0';
    const variant = {
      primary: 'bg-clinic font-bold text-white hover:bg-clinic-deep',
      ghost: 'bg-white font-normal text-ink ring-1 ring-rule hover:bg-mist',
      danger: 'bg-pulse font-bold text-white hover:bg-pulse-deep',
    }[this.variant()];
    return `${base} ${variant}`;
  });
}

@Component({
  selector: 'ui-field',
  host: { class: 'block min-w-0' },
  template: `
    <label class="block text-sm">
      <span class="mb-1.5 block text-ink/75">{{ label() }}</span>
      <ng-content />
    </label>
  `,
})
export class UiField {
  readonly label = input('');
}

@Component({
  selector: 'ui-table',
  template: `
    <div class="overflow-x-auto border border-rule bg-white">
      <table class="min-w-full border-separate border-spacing-0 text-left text-sm [&_tbody_tr]:transition-colors [&_tbody_tr]:duration-200 [&_tbody_tr:hover]:bg-paper">
        <ng-content />
      </table>
    </div>
  `,
})
export class UiTable {}

@Component({
  selector: 'ui-modal',
  template: `
    @if (open()) {
      <div class="backdrop-fade fixed inset-0 z-40 flex items-end justify-center bg-ink/45 p-4 sm:items-center" (click)="closed.emit()">
        <div
          class="dialog-pop w-full max-w-lg border border-rule bg-white p-5 sm:p-6"
          role="dialog"
          [attr.aria-label]="title()"
          (click)="$event.stopPropagation()"
        >
          <h2 class="mb-5 font-serif text-2xl tracking-tight">{{ title() }}</h2>
          <ng-content />
        </div>
      </div>
    }
  `,
})
export class UiModal {
  readonly open = input(false);
  readonly title = input('');
  readonly closed = output<void>();
}

@Component({
  selector: 'ui-badge',
  template: `<span [class]="classes()"><ng-content /></span>`,
})
export class UiBadge {
  readonly tone = input<'ok' | 'warn' | 'muted'>('muted');
  readonly classes = computed(() => {
    const tone = {
      ok: 'bg-clinic-wash text-clinic-deep',
      warn: 'bg-ochre-wash text-ochre',
      muted: 'bg-mist text-ink/80',
    }[this.tone()];
    return `inline-flex rounded-sm px-2 py-0.5 text-xs font-bold ${tone}`;
  });
}

@Component({
  selector: 'ui-empty',
  template: `
    <div class="rise border-t border-rule py-10 text-sm leading-relaxed text-ink/65">
      {{ message() }}
    </div>
  `,
})
export class UiEmpty {
  readonly message = input('No hay datos para mostrar.');
}

@Component({
  selector: 'ui-calendar',
  template: `
    <div class="flex flex-col gap-4 border-b border-rule pb-5 sm:flex-row sm:items-end sm:justify-between">
      <label class="block w-full text-sm sm:max-w-56">
        <span class="mb-1.5 block text-ink/75">Fecha</span>
        <input type="date" [value]="fecha()" (change)="onChange($event)" />
      </label>
      <div class="flex flex-wrap gap-2">
        <ng-content />
      </div>
    </div>
  `,
})
export class UiCalendar {
  readonly fecha = input('');
  readonly fechaChange = output<string>();

  onChange(event: Event) {
    this.fechaChange.emit((event.target as HTMLInputElement).value);
  }
}
