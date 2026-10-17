import { Component, input } from '@angular/core';

@Component({
  selector: 'screen-note',
  host: { class: 'block' },
  template: `
    <section class="screen-note">
      <p>{{ loading() }}</p>
      <p><span>Por qué lo editás.</span> {{ reason() }}</p>
      <p><span>Si no lo cargás.</span> {{ skip() }}</p>
    </section>
  `,
})
export class ScreenNote {
  readonly loading = input.required<string>();
  readonly reason = input.required<string>();
  readonly skip = input.required<string>();
}
