import { Component, input } from '@angular/core';

// The professional's stamp as it goes on a signed page: their handwritten signature if they added one, then
// name, specialty and license, like a paper stamp. It shows who signed; what binds them is the electronic signature.
@Component({
  selector: 'app-stamp',
  template: `
    <figure class="inline-flex w-56 max-w-full flex-col items-center text-center" [attr.aria-label]="'Firma y sello de ' + name()">
      <div class="flex h-14 w-full items-end justify-center">
        @if (image()) {
          <img class="max-h-14 max-w-full mix-blend-multiply" [src]="image()" alt="Firma ológrafa" />
        }
      </div>
      <figcaption class="mt-1 w-full border-t border-ink/50 pt-1 leading-tight">
        <span class="block text-sm font-semibold">{{ name() }}</span>
        <span class="block text-xs text-ink/75">{{ specialty() }}</span>
        @if (license()) {
          <span class="block text-xs text-ink/75">Matrícula {{ license() }}</span>
        }
        <span class="mt-0.5 block text-[0.65rem] text-ink/50">Firmado electrónicamente</span>
      </figcaption>
    </figure>
  `,
})
export class Stamp {
  readonly name = input.required<string>();
  readonly specialty = input('');
  readonly license = input<string | null | undefined>(null);
  readonly image = input<string | null | undefined>(null);
}
